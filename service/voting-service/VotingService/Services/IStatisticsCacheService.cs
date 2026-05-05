using VotingService.DTOs;

namespace VotingService.Services;

public interface IStatisticsCacheService
{
    Task<SurveyVoteResultsResponse?> TryGetSurveyResultsAsync(int surveyId, CancellationToken cancellationToken);
    Task SetSurveyResultsAsync(SurveyVoteResultsResponse results, CancellationToken cancellationToken);
    Task InvalidateSurveyResultsAsync(int surveyId, CancellationToken cancellationToken);
    Task PublishVoteCreatedEventAsync(int surveyId, int voteId, CancellationToken cancellationToken);
}
