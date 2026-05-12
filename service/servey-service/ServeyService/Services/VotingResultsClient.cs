using System.Net.Http.Json;
using VotingService.DTOs;

namespace SurveyService.Services;

public interface IVotingResultsClient
{
    Task<SurveyVoteResultsResponse?> GetSurveyResultsAsync(int surveyId, CancellationToken cancellationToken);
}

public sealed class VotingResultsClient(HttpClient httpClient) : IVotingResultsClient
{
    public Task<SurveyVoteResultsResponse?> GetSurveyResultsAsync(int surveyId, CancellationToken cancellationToken)
        => httpClient.GetFromJsonAsync<SurveyVoteResultsResponse>(
            $"/api/votes/surveys/{surveyId}/results",
            cancellationToken);
}

