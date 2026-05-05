using System.ComponentModel.DataAnnotations;

namespace VotingService.DTOs;

public sealed class SubmitVoteRequest
{
    [Range(1, int.MaxValue)]
    public int SurveyId { get; set; }

    [Range(1, int.MaxValue)]
    public int VoterId { get; set; }

    [MinLength(1)]
    public List<SubmitVoteAnswerRequest> Answers { get; set; } = new();
}

public sealed class SubmitVoteAnswerRequest
{
    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Range(1, int.MaxValue)]
    public int OptionId { get; set; }
}

public sealed class SurveyVoteResultsResponse
{
    public int SurveyId { get; set; }
    public int TotalVotes { get; set; }
    public List<QuestionVoteResultsResponse> Questions { get; set; } = new();
}

public sealed class QuestionVoteResultsResponse
{
    public int QuestionId { get; set; }
    public List<OptionVoteResultsResponse> Options { get; set; } = new();
}

public sealed class OptionVoteResultsResponse
{
    public int OptionId { get; set; }
    public int VotesCount { get; set; }
}
