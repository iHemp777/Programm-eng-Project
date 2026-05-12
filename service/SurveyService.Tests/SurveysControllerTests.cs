using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SurveyService.Controllers;
using SurveyService.Data;
using SurveyService.DTOs;
using SurveyService.Models;
using SurveyService.Services;
using Xunit;

namespace SurveyService.Tests;

public class SurveysControllerTests
{
    [Fact]
    public async Task CreateSurvey_GeneratesInviteToken_ForPrivateSurvey()
    {
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var request = new CreateSurveyRequest
        {
            Title = "Private Survey",
            AccessType = Survey.SurveyAccessType.PrivateByLink,
            Status = Survey.SurveyStatus.Published,
            Questions =
            [
                new CreateQuestionRequest
                {
                    Text = "Q1",
                    Order = 1,
                    IsRequired = true,
                    Options =
                    [
                        new CreateOptionRequest { Text = "A", Order = 1 }
                    ]
                }
            ]
        };

        var result = await controller.CreateSurvey(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(SurveysController.GetSurvey), created.ActionName);
        var survey = await db.Surveys.SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(survey.InviteToken));
        Assert.Equal(32, survey.InviteToken!.Length);
    }

    [Fact]
    public async Task UpdateSurvey_SetsCompletedAt_WhenStatusClosedAndCompletedAtMissing()
    {
        await using var db = CreateDbContext();
        db.Surveys.Add(new Survey
        {
            Title = "Test survey",
            Status = Survey.SurveyStatus.Published,
            AccessType = Survey.SurveyAccessType.Public,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var surveyId = await db.Surveys.Select(s => s.Id).SingleAsync();
        var controller = CreateController(db);

        var request = new UpdateSurveyRequest
        {
            Id = surveyId,
            Title = "Test survey updated",
            Status = Survey.SurveyStatus.Closed,
            AccessType = Survey.SurveyAccessType.Public,
            IsActive = true,
            IsAnonymous = false
        };

        var response = await controller.UpdateSurvey(surveyId, request);

        Assert.IsType<NoContentResult>(response);
        var updatedSurvey = await db.Surveys.SingleAsync();
        Assert.NotNull(updatedSurvey.CompletedAt);
    }

    [Fact]
    public async Task GetPublicSurveys_ReturnsOnlyActivePublicSurveysInTimeWindow()
    {
        await using var db = CreateDbContext();
        var now = DateTime.UtcNow;

        db.Surveys.AddRange(
            new Survey
            {
                Title = "public active",
                CreatedAt = now,
                IsActive = true,
                AccessType = Survey.SurveyAccessType.Public,
                StartsAt = now.AddDays(-1),
                EndsAt = now.AddDays(1)
            },
            new Survey
            {
                Title = "private active",
                CreatedAt = now,
                IsActive = true,
                AccessType = Survey.SurveyAccessType.PrivateByLink,
                StartsAt = now.AddDays(-1),
                EndsAt = now.AddDays(1)
            },
            new Survey
            {
                Title = "public inactive",
                CreatedAt = now,
                IsActive = false,
                AccessType = Survey.SurveyAccessType.Public,
                StartsAt = now.AddDays(-1),
                EndsAt = now.AddDays(1)
            },
            new Survey
            {
                Title = "public future",
                CreatedAt = now,
                IsActive = true,
                AccessType = Survey.SurveyAccessType.Public,
                StartsAt = now.AddDays(1),
                EndsAt = now.AddDays(2)
            });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetPublicSurveys();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<SurveySummaryResponse>>(ok.Value);
        var list = payload.ToList();
        Assert.Single(list);
        Assert.Equal("public active", list[0].Title);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static SurveysController CreateController(AppDbContext db)
        => new(db, NullLogger<SurveysController>.Instance, new FakePredictionScoringService());

    private sealed class FakePredictionScoringService : IPredictionScoringService
    {
        public Task<int> ScoreSurveyPredictionsAsync(int surveyId, CancellationToken cancellationToken)
            => Task.FromResult(0);
    }
}
