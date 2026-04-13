using System.ComponentModel.DataAnnotations;

namespace VotingService.Models;

public class Vote
{
    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int SurveyId { get; set; }

    [Range(1, int.MaxValue)]
    public int VoterId { get; set; }

    public DateTime VotedAt { get; set; }

    public List<VoteAnswer> Answers { get; set; } = new();
}
