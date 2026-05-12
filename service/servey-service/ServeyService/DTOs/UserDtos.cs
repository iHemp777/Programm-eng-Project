using System.ComponentModel.DataAnnotations;

namespace SurveyService.DTOs;

public sealed class UpdateUserProfileRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string DisplayName { get; set; } = "User";

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }
}

public sealed class UserProfileResponse
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Points { get; set; }
    public List<UserPublishedSurveyResponse> PublishedSurveys { get; set; } = new();
}

public sealed class UserPublishedSurveyResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool EnablePredictions { get; set; }
}

