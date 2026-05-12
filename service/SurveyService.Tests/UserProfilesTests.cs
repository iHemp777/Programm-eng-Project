using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyService.Controllers;
using SurveyService.Data;
using SurveyService.DTOs;
using SurveyService.Models;
using Xunit;

namespace SurveyService.Tests;

public class UserProfilesTests
{
    [Fact]
    public async Task GetProfile_ReturnsPointsAndPublishedSurveys()
    {
        await using var db = CreateDbContext();

        db.UserProfiles.Add(new UserProfile { UserId = 5, DisplayName = "Alice", AvatarUrl = "http://img" });
        db.UserScores.Add(new UserScore { UserId = 5, Points = 7 });
        db.Surveys.AddRange(
            new Survey { Title = "pub1", CreatedAt = DateTime.UtcNow, CreatedBy = 5, Status = Survey.SurveyStatus.Published, AccessType = Survey.SurveyAccessType.Public },
            new Survey { Title = "draft", CreatedAt = DateTime.UtcNow, CreatedBy = 5, Status = Survey.SurveyStatus.Draft, AccessType = Survey.SurveyAccessType.Public },
            new Survey { Title = "pub2", CreatedAt = DateTime.UtcNow, CreatedBy = 5, Status = Survey.SurveyStatus.Published, AccessType = Survey.SurveyAccessType.Public }
        );
        await db.SaveChangesAsync();

        var controller = new UsersController(db);
        var result = await controller.GetProfile(5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var profile = Assert.IsType<UserProfileResponse>(ok.Value);
        Assert.Equal(5, profile.UserId);
        Assert.Equal("Alice", profile.DisplayName);
        Assert.Equal("http://img", profile.AvatarUrl);
        Assert.Equal(7, profile.Points);
        Assert.Equal(2, profile.PublishedSurveys.Count);
        Assert.All(profile.PublishedSurveys, s => Assert.False(string.IsNullOrWhiteSpace(s.Title)));
    }

    [Fact]
    public async Task UpsertProfile_CreatesOrUpdates()
    {
        await using var db = CreateDbContext();
        var controller = new UsersController(db);

        var req = new UpdateUserProfileRequest { UserId = 9, DisplayName = "Bob", AvatarUrl = "" };
        var res = await controller.UpsertProfile(9, req, CancellationToken.None);
        Assert.IsType<NoContentResult>(res);

        var saved = await db.UserProfiles.SingleAsync(p => p.UserId == 9);
        Assert.Equal("Bob", saved.DisplayName);
        Assert.Null(saved.AvatarUrl);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}

