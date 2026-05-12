using System.ComponentModel.DataAnnotations;

namespace SurveyService.Models;

public sealed class UserScore
{
    [Key]
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Range(0, int.MaxValue)]
    public int Points { get; set; } = 0;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

