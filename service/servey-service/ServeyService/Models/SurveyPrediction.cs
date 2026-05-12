using System.ComponentModel.DataAnnotations;

namespace SurveyService.Models;

public sealed class SurveyPrediction
{
    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int SurveyId { get; set; }

    public Survey? Survey { get; set; }

    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsScored { get; set; } = false;

    [Range(0, int.MaxValue)]
    public int Score { get; set; } = 0;

    public DateTime? ScoredAt { get; set; }

    [MinLength(1)]
    public List<SurveyPredictionAnswer> Answers { get; set; } = new();
}

