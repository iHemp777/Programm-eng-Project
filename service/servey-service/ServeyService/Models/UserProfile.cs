using System.ComponentModel.DataAnnotations;

namespace SurveyService.Models;

public sealed class UserProfile
{
    [Key]
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string DisplayName { get; set; } = "User";

    [MaxLength(512)]
    public string? AvatarUrl { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

