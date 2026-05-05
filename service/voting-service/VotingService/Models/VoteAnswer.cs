using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VotingService.Models;

public class VoteAnswer
{
    [Key]
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Range(1, int.MaxValue)]
    public int OptionId { get; set; }

    public int VoteId { get; set; }

    [JsonIgnore]
    public Vote? Vote { get; set; }
}
