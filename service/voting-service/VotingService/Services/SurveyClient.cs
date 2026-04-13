namespace VotingService.Services;

public class SurveyClient(HttpClient httpClient) : ISurveyClient
{
    public async Task<bool> SurveyExistsAsync(int surveyId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/api/surveys/{surveyId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
