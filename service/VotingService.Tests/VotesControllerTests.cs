using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VotingService.Controllers;
using VotingService.Data;
using VotingService.DTOs;
using VotingService.Models;
using VotingService.Services;
using Xunit;

namespace VotingService.Tests;

public class VotesControllerTests
{
    [Fact]
    public async Task SubmitVote_ReturnsBadRequest_WhenDuplicateQuestionAnswersProvided()
    {
        await using var db = CreateDbContext();
        var controller = new VotesController(db, new FakeSurveyClient(true), new FakeStatisticsCacheService());

        var request = new SubmitVoteRequest
        {
            SurveyId = 10,
            VoterId = 100,
            Answers =
            [
                new SubmitVoteAnswerRequest { QuestionId = 1, OptionId = 11 },
                new SubmitVoteAnswerRequest { QuestionId = 1, OptionId = 12 }
            ]
        };

        var result = await controller.SubmitVote(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task SubmitVote_ReturnsConflict_WhenUserAlreadyVoted()
    {
        await using var db = CreateDbContext();
        db.Votes.Add(new Vote
        {
            SurveyId = 11,
            VoterId = 101,
            VotedAt = DateTime.UtcNow,
            Answers = [new VoteAnswer { QuestionId = 1, OptionId = 1 }]
        });
        await db.SaveChangesAsync();

        var controller = new VotesController(db, new FakeSurveyClient(true), new FakeStatisticsCacheService());

        var request = new SubmitVoteRequest
        {
            SurveyId = 11,
            VoterId = 101,
            Answers = [new SubmitVoteAnswerRequest { QuestionId = 1, OptionId = 2 }]
        };

        var result = await controller.SubmitVote(request, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSurveyResults_BuildsAndReturnsAggregatedResults_WhenCacheMiss()
    {
        await using var db = CreateDbContext();
        db.Votes.AddRange(
            new Vote
            {
                SurveyId = 15,
                VoterId = 201,
                VotedAt = DateTime.UtcNow,
                Answers = [new VoteAnswer { QuestionId = 1, OptionId = 10 }]
            },
            new Vote
            {
                SurveyId = 15,
                VoterId = 202,
                VotedAt = DateTime.UtcNow,
                Answers = [new VoteAnswer { QuestionId = 1, OptionId = 10 }]
            },
            new Vote
            {
                SurveyId = 15,
                VoterId = 203,
                VotedAt = DateTime.UtcNow,
                Answers = [new VoteAnswer { QuestionId = 1, OptionId = 11 }]
            });
        await db.SaveChangesAsync();

        var cache = new FakeStatisticsCacheService();
        var controller = new VotesController(db, new FakeSurveyClient(true), cache);

        var result = await controller.GetSurveyResults(15, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<SurveyVoteResultsResponse>(ok.Value);
        Assert.Equal(15, payload.SurveyId);
        Assert.Equal(3, payload.TotalVotes);
        Assert.Single(payload.Questions);
        Assert.Equal(2, payload.Questions[0].Options.Count);
        Assert.Equal(2, payload.Questions[0].Options.First(x => x.OptionId == 10).VotesCount);
        Assert.NotNull(cache.LastSetResults);
        Assert.Equal(15, cache.LastSetResults!.SurveyId);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class FakeSurveyClient(bool exists) : ISurveyClient
    {
        public Task<bool> SurveyExistsAsync(int surveyId, CancellationToken cancellationToken) => Task.FromResult(exists);
    }

    private sealed class FakeStatisticsCacheService : IStatisticsCacheService
    {
        public SurveyVoteResultsResponse? LastSetResults { get; private set; }

        public Task<SurveyVoteResultsResponse?> TryGetSurveyResultsAsync(int surveyId, CancellationToken cancellationToken)
            => Task.FromResult<SurveyVoteResultsResponse?>(null);

        public Task SetSurveyResultsAsync(SurveyVoteResultsResponse results, CancellationToken cancellationToken)
        {
            LastSetResults = results;
            return Task.CompletedTask;
        }

        public Task InvalidateSurveyResultsAsync(int surveyId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PublishVoteCreatedEventAsync(int surveyId, int voteId, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
