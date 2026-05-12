using Microsoft.EntityFrameworkCore;
using SurveyService.Data;
using SurveyService.Models;

namespace SurveyService.Services;

public interface IPredictionScoringService
{
    Task<int> ScoreSurveyPredictionsAsync(int surveyId, CancellationToken cancellationToken);
}

public sealed class PredictionScoringService(
    AppDbContext db,
    IVotingResultsClient votingResultsClient) : IPredictionScoringService
{
    public async Task<int> ScoreSurveyPredictionsAsync(int surveyId, CancellationToken cancellationToken)
    {
        var survey = await db.Surveys
            .Include(s => s.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s => s.Id == surveyId, cancellationToken);

        if (survey == null)
            throw new InvalidOperationException($"Survey with ID {surveyId} was not found.");

        if (survey.AccessType != Survey.SurveyAccessType.Public || survey.EndsAt == null)
            throw new InvalidOperationException("Predictions scoring is supported only for public time-limited surveys.");

        if (DateTime.UtcNow < survey.EndsAt.Value)
            throw new InvalidOperationException("Survey is not finished yet.");

        var results = await votingResultsClient.GetSurveyResultsAsync(surveyId, cancellationToken);
        if (results == null)
            throw new InvalidOperationException("Voting results are not available.");

        var leadersByQuestion = results.Questions
            .ToDictionary(
                q => q.QuestionId,
                q =>
                {
                    var leader = q.Options
                        .OrderByDescending(o => o.VotesCount)
                        .ThenBy(o => o.OptionId)
                        .FirstOrDefault();
                    return leader?.OptionId;
                });

        var predictionsToScore = await db.SurveyPredictions
            .Include(p => p.Answers)
            .Where(p => p.SurveyId == surveyId && !p.IsScored)
            .ToListAsync(cancellationToken);

        foreach (var prediction in predictionsToScore)
        {
            var score = 0;
            foreach (var answer in prediction.Answers)
            {
                if (leadersByQuestion.TryGetValue(answer.QuestionId, out var leaderOptionId) &&
                    leaderOptionId != null &&
                    leaderOptionId.Value == answer.OptionId)
                {
                    score++;
                }
            }

            prediction.IsScored = true;
            prediction.Score = score;
            prediction.ScoredAt = DateTime.UtcNow;

            if (score > 0)
            {
                var userScore = await db.UserScores
                    .FirstOrDefaultAsync(u => u.UserId == prediction.UserId, cancellationToken);

                if (userScore == null)
                {
                    userScore = new UserScore { UserId = prediction.UserId, Points = 0 };
                    db.UserScores.Add(userScore);
                }

                userScore.Points += score;
                userScore.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return predictionsToScore.Count;
    }
}

