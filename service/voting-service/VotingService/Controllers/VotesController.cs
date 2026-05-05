using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VotingService.Data;
using VotingService.DTOs;
using VotingService.Models;
using VotingService.Services;

namespace VotingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VotesController(
    AppDbContext context,
    ISurveyClient surveyClient,
    IStatisticsCacheService statisticsCacheService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<object>> SubmitVote([FromBody] SubmitVoteRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Answers.Select(a => a.QuestionId).Distinct().Count() != request.Answers.Count)
        {
            return BadRequest("Duplicate answers for the same question are not allowed.");
        }

        var surveyExists = await surveyClient.SurveyExistsAsync(request.SurveyId, cancellationToken);
        if (!surveyExists)
        {
            return NotFound($"Survey with ID {request.SurveyId} was not found.");
        }

        var alreadyVoted = await context.Votes
            .AnyAsync(v => v.SurveyId == request.SurveyId && v.VoterId == request.VoterId, cancellationToken);

        if (alreadyVoted)
        {
            return Conflict("You have already voted in this survey.");
        }

        var vote = new Vote
        {
            SurveyId = request.SurveyId,
            VoterId = request.VoterId,
            VotedAt = DateTime.UtcNow,
            Answers = request.Answers.Select(a => new VoteAnswer
            {
                QuestionId = a.QuestionId,
                OptionId = a.OptionId
            }).ToList()
        };

        context.Votes.Add(vote);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("You have already voted in this survey.");
        }

        await statisticsCacheService.InvalidateSurveyResultsAsync(request.SurveyId, cancellationToken);
        await statisticsCacheService.PublishVoteCreatedEventAsync(request.SurveyId, vote.Id, cancellationToken);

        return CreatedAtAction(nameof(GetSurveyResults), new { surveyId = request.SurveyId }, new
        {
            vote.Id,
            vote.SurveyId,
            vote.VoterId,
            vote.VotedAt
        });
    }

    [HttpGet("surveys/{surveyId:int}/results")]
    [ProducesResponseType(typeof(SurveyVoteResultsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyVoteResultsResponse>> GetSurveyResults(int surveyId, CancellationToken cancellationToken)
    {
        var surveyExists = await surveyClient.SurveyExistsAsync(surveyId, cancellationToken);
        if (!surveyExists)
        {
            return NotFound($"Survey with ID {surveyId} was not found.");
        }

        var cachedResults = await statisticsCacheService.TryGetSurveyResultsAsync(surveyId, cancellationToken);
        if (cachedResults != null)
        {
            return Ok(cachedResults);
        }

        var totalVotes = await context.Votes
            .CountAsync(v => v.SurveyId == surveyId, cancellationToken);

        var grouped = await context.VoteAnswers
            .Where(a => a.Vote != null && a.Vote.SurveyId == surveyId)
            .GroupBy(a => new { a.QuestionId, a.OptionId })
            .Select(g => new
            {
                g.Key.QuestionId,
                g.Key.OptionId,
                VotesCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var result = new SurveyVoteResultsResponse
        {
            SurveyId = surveyId,
            TotalVotes = totalVotes,
            Questions = grouped
                .GroupBy(x => x.QuestionId)
                .Select(g => new QuestionVoteResultsResponse
                {
                    QuestionId = g.Key,
                    Options = g.Select(x => new OptionVoteResultsResponse
                    {
                        OptionId = x.OptionId,
                        VotesCount = x.VotesCount
                    })
                    .OrderBy(x => x.OptionId)
                    .ToList()
                })
                .OrderBy(x => x.QuestionId)
                .ToList()
        };

        await statisticsCacheService.SetSurveyResultsAsync(result, cancellationToken);
        return Ok(result);
    }

    [HttpGet("surveys/{surveyId:int}/has-voted")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> HasVoted(
        int surveyId,
        [FromQuery] int voterId,
        CancellationToken cancellationToken)
    {
        var hasVoted = await context.Votes
            .AnyAsync(v => v.SurveyId == surveyId && v.VoterId == voterId, cancellationToken);

        return Ok(new
        {
            SurveyId = surveyId,
            VoterId = voterId,
            HasVoted = hasVoted
        });
    }
}
