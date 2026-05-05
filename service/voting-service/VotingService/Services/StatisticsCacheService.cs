using System.Text.Json;
using StackExchange.Redis;
using VotingService.DTOs;

namespace VotingService.Services;

public class StatisticsCacheService(IConnectionMultiplexer redis) : IStatisticsCacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly ISubscriber _subscriber = redis.GetSubscriber();

    public async Task<SurveyVoteResultsResponse?> TryGetSurveyResultsAsync(int surveyId, CancellationToken cancellationToken)
    {
        var key = BuildResultsKey(surveyId);
        var cachedJson = await _db.StringGetAsync(key);

        if (cachedJson.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<SurveyVoteResultsResponse>(cachedJson!);
    }

    public async Task SetSurveyResultsAsync(SurveyVoteResultsResponse results, CancellationToken cancellationToken)
    {
        var key = BuildResultsKey(results.SurveyId);
        var json = JsonSerializer.Serialize(results);
        await _db.StringSetAsync(key, json, expiry: TimeSpan.FromMinutes(5));
    }

    public async Task InvalidateSurveyResultsAsync(int surveyId, CancellationToken cancellationToken)
    {
        var key = BuildResultsKey(surveyId);
        await _db.KeyDeleteAsync(key);
    }

    public async Task PublishVoteCreatedEventAsync(int surveyId, int voteId, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            SurveyId = surveyId,
            VoteId = voteId,
            OccurredAt = DateTime.UtcNow
        });

        await _subscriber.PublishAsync(RedisChannel.Literal("votes:created"), payload);
    }

    private static string BuildResultsKey(int surveyId) => $"survey:{surveyId}:results";
}
