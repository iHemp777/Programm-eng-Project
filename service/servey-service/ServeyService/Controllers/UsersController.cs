using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyService.Data;
using SurveyService.DTOs;
using SurveyService.Models;

namespace SurveyService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(int userId, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var score = await db.UserScores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        var published = await db.Surveys
            .AsNoTracking()
            .Where(s => s.CreatedBy == userId && s.Status == Survey.SurveyStatus.Published)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new UserPublishedSurveyResponse
            {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                EndsAt = s.EndsAt,
                EnablePredictions = s.EnablePredictions
            })
            .ToListAsync(cancellationToken);

        return Ok(new UserProfileResponse
        {
            UserId = userId,
            DisplayName = profile?.DisplayName ?? $"User {userId}",
            AvatarUrl = profile?.AvatarUrl,
            Points = score?.Points ?? 0,
            PublishedSurveys = published
        });
    }

    [HttpPut("{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertProfile(int userId, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (userId != request.UserId)
            return BadRequest("ID in URL does not match request body.");

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            db.UserProfiles.Add(profile);
        }

        profile.DisplayName = request.DisplayName.Trim();
        profile.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

