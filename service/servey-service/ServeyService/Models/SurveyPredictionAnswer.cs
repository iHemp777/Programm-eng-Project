using System.ComponentModel.DataAnnotations;

namespace SurveyService.Models;

public sealed class SurveyPredictionAnswer
{
    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int PredictionId { get; set; }

    public SurveyPrediction? Prediction { get; set; }

    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Range(1, int.MaxValue)]
    public int OptionId { get; set; }
}

