namespace VotingService.Services;

public interface ISurveyClient
{
    Task<bool> SurveyExistsAsync(int surveyId, CancellationToken cancellationToken);
}
