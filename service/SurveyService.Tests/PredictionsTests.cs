using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SurveyService.Controllers;
using SurveyService.Data;
using SurveyService.DTOs;
using SurveyService.Models;
using SurveyService.Services;
using VotingService.DTOs;
using Xunit;

namespace SurveyService.Tests;

public class PredictionsTests
{
    [Fact]
    public async Task CreatePrediction_ReturnsCreated_WhenValidPublicTimeLimitedSurvey()
    {
        await using var db = CreateDbContext();
        var surveyId = await SeedSurveyAsync(db, endsAtUtc: DateTime.UtcNow.AddHours(2), enablePredictions: true);

        var scoring = new PredictionScoringService(db, new FakeVotingResultsClient(new SurveyVoteResultsResponse()));
        var controller = new SurveysController(db, NullLogger<SurveysController>.Instance, scoring);

        var survey = await db.Surveys.Include(s => s.Questions).ThenInclude(q => q.Options).SingleAsync(s => s.Id == surveyId);
        var q1 = survey.Questions.Single();
        var opt1 = q1.Options.Single();

        var request = new CreateSurveyPredictionRequest
        {
            UserId = 77,
            Answers =
            [
                new CreateSurveyPredictionAnswerRequest { QuestionId = q1.Id, OptionId = opt1.Id }
            ]
        };

        var result = await controller.CreatePrediction(surveyId, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var payload = Assert.IsType<SurveyPredictionResponse>(created.Value);
        Assert.Equal(surveyId, payload.SurveyId);
        Assert.Equal(77, payload.UserId);
        Assert.Single(payload.Answers);
    }

    [Fact]
    public async Task CreatePrediction_ReturnsConflict_WhenPredictionAlreadyExists()
    {
        await using var db = CreateDbContext();
        var surveyId = await SeedSurveyAsync(db, endsAtUtc: DateTime.UtcNow.AddHours(2), enablePredictions: true);
        var survey = await db.Surveys.Include(s => s.Questions).ThenInclude(q => q.Options).SingleAsync(s => s.Id == surveyId);
        var q1 = survey.Questions.Single();
        var opt1 = q1.Options.Single();

        db.SurveyPredictions.Add(new SurveyPrediction
        {
            SurveyId = surveyId,
            UserId = 10,
            CreatedAt = DateTime.UtcNow,
            Answers = [new SurveyPredictionAnswer { QuestionId = q1.Id, OptionId = opt1.Id }]
        });
        await db.SaveChangesAsync();

        var scoring = new PredictionScoringService(db, new FakeVotingResultsClient(new SurveyVoteResultsResponse()));
        var controller = new SurveysController(db, NullLogger<SurveysController>.Instance, scoring);

        var request = new CreateSurveyPredictionRequest
        {
            UserId = 10,
            Answers = [new CreateSurveyPredictionAnswerRequest { QuestionId = q1.Id, OptionId = opt1.Id }]
        };

        var result = await controller.CreatePrediction(surveyId, request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task ScorePredictions_AddsPointsAndMarksScored_Idempotent()
    {
        await using var db = CreateDbContext();
        var surveyId = await SeedSurveyAsync(db, endsAtUtc: DateTime.UtcNow.AddMinutes(-5), enablePredictions: true);
        var survey = await db.Surveys.Include(s => s.Questions).ThenInclude(q => q.Options).SingleAsync(s => s.Id == surveyId);
        var q1 = survey.Questions.Single();
        var opt1 = q1.Options.Single();

        db.SurveyPredictions.Add(new SurveyPrediction
        {
            SurveyId = surveyId,
            UserId = 123,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            Answers = [new SurveyPredictionAnswer { QuestionId = q1.Id, OptionId = opt1.Id }]
        });
        await db.SaveChangesAsync();

        var results = new SurveyVoteResultsResponse
        {
            SurveyId = surveyId,
            TotalVotes = 1,
            Questions =
            [
                new QuestionVoteResultsResponse
                {
                    QuestionId = q1.Id,
                    Options =
                    [
                        new OptionVoteResultsResponse { OptionId = opt1.Id, VotesCount = 10 }
                    ]
                }
            ]
        };

        var scoring = new PredictionScoringService(db, new FakeVotingResultsClient(results));
        var controller = new SurveysController(db, NullLogger<SurveysController>.Instance, scoring);

        var first = await controller.ScorePredictions(surveyId, CancellationToken.None);
        var ok1 = Assert.IsType<OkObjectResult>(first.Result);
        Assert.Contains("ScoredPredictions", ok1.Value!.ToString());

        var userScore = await db.UserScores.SingleAsync(u => u.UserId == 123);
        Assert.Equal(1, userScore.Points);
        var prediction = await db.SurveyPredictions.SingleAsync();
        Assert.True(prediction.IsScored);
        Assert.Equal(1, prediction.Score);

        var second = await controller.ScorePredictions(surveyId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(second.Result);

        var userScore2 = await db.UserScores.SingleAsync(u => u.UserId == 123);
        Assert.Equal(1, userScore2.Points);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<int> SeedSurveyAsync(AppDbContext db, DateTime endsAtUtc, bool enablePredictions)
    {
        var now = DateTime.UtcNow;

        var survey = new Survey
        {
            Title = "Predictable survey",
            CreatedAt = now,
            Status = Survey.SurveyStatus.Published,
            AccessType = Survey.SurveyAccessType.Public,
            IsActive = true,
            StartsAt = now.AddHours(-1),
            EndsAt = endsAtUtc,
            EnablePredictions = enablePredictions,
            Questions =
            [
                new Question
                {
                    Text = "Q1",
                    Order = 1,
                    IsRequired = true,
                    Options =
                    [
                        new Option { Text = "A", Order = 1 }
                    ]
                }
            ]
        };

        db.Surveys.Add(survey);
        await db.SaveChangesAsync();
        return survey.Id;
    }

    [Fact]
    public async Task CreatePrediction_ReturnsBadRequest_WhenPredictionsDisabled()
    {
        await using var db = CreateDbContext();
        var surveyId = await SeedSurveyAsync(db, endsAtUtc: DateTime.UtcNow.AddHours(2), enablePredictions: false);

        var scoring = new PredictionScoringService(db, new FakeVotingResultsClient(new SurveyVoteResultsResponse()));
        var controller = new SurveysController(db, NullLogger<SurveysController>.Instance, scoring);

        var survey = await db.Surveys.Include(s => s.Questions).ThenInclude(q => q.Options).SingleAsync(s => s.Id == surveyId);
        var q1 = survey.Questions.Single();
        var opt1 = q1.Options.Single();

        var request = new CreateSurveyPredictionRequest
        {
            UserId = 1,
            Answers = [new CreateSurveyPredictionAnswerRequest { QuestionId = q1.Id, OptionId = opt1.Id }]
        };

        var result = await controller.CreatePrediction(surveyId, request, CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("not enabled", bad.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePrediction_ReturnsBadRequest_WhenNotAllQuestionsPredicted()
    {
        await using var db = CreateDbContext();
        var now = DateTime.UtcNow;
        var survey = new Survey
        {
            Title = "Multi",
            CreatedAt = now,
            Status = Survey.SurveyStatus.Published,
            AccessType = Survey.SurveyAccessType.Public,
            IsActive = true,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddHours(2),
            EnablePredictions = true,
            Questions =
            [
                new Question
                {
                    Text = "Q1",
                    Order = 1,
                    IsRequired = true,
                    Options = [new Option { Text = "A", Order = 1 }]
                },
                new Question
                {
                    Text = "Q2",
                    Order = 2,
                    IsRequired = true,
                    Options = [new Option { Text = "B", Order = 1 }]
                }
            ]
        };
        db.Surveys.Add(survey);
        await db.SaveChangesAsync();

        var scoring = new PredictionScoringService(db, new FakeVotingResultsClient(new SurveyVoteResultsResponse()));
        var controller = new SurveysController(db, NullLogger<SurveysController>.Instance, scoring);

        var q1 = survey.Questions[0];
        var opt1 = q1.Options[0];

        var request = new CreateSurveyPredictionRequest
        {
            UserId = 2,
            Answers = [new CreateSurveyPredictionAnswerRequest { QuestionId = q1.Id, OptionId = opt1.Id }]
        };

        var result = await controller.CreatePrediction(survey.Id, request, CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("exactly one leader for each question", bad.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeVotingResultsClient(SurveyVoteResultsResponse? response) : IVotingResultsClient
    {
        public Task<SurveyVoteResultsResponse?> GetSurveyResultsAsync(int surveyId, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}

