using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyService.Data;
using SurveyService.DTOs;
using SurveyService.Models;
using SurveyService.Services;

namespace SurveyService.Controllers;

/// <summary>
/// API-    .
///
///  :
/// -  HTTP- (REST)   .
/// -      (ModelState +  ).
/// -    ,    :
///   - `CreatedAt`, `UpdatedAt`, `InviteToken`,  `CompletedAt`.
/// -     `AppDbContext` (EF Core).
///
/// :
/// -     DTO `SurveySummaryResponse` ( /),
///            .
/// -     (`GetSurvey` / `GetPrivateSurveyByToken`) 
///   Entity `Survey`  `Questions`  `Options`.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SurveysController> _logger;
    private readonly IPredictionScoringService _predictionScoringService;

    public SurveysController(AppDbContext context, ILogger<SurveysController> logger, IPredictionScoringService predictionScoringService)
    {
        _context = context;
        _logger = logger;
        _predictionScoringService = predictionScoringService;
    }

    // ========== GET: api/surveys ==========
    /// <summary>
    ///     (  /).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SurveySummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SurveySummaryResponse>>> GetSurveys()
    {
        var surveys = await _context.Surveys
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => ToSummary(s))
            .ToListAsync();

        return Ok(surveys);
    }

    // ========== GET: api/surveys/public ==========
    /// <summary>
    ///     ( ).
    ///
    /// :
    /// -    `status`
    /// -     (`activeOnly=true`)
    /// -    `StartsAt`/`EndsAt` ( )
    /// </summary>
    [HttpGet("public")]
    [ProducesResponseType(typeof(IEnumerable<SurveySummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SurveySummaryResponse>>> GetPublicSurveys(
        [FromQuery] Survey.SurveyStatus? status = null,
        [FromQuery] bool activeOnly = true)
    {
        var now = DateTime.UtcNow;

        var query = _context.Surveys.AsQueryable();

        query = query.Where(s => s.AccessType == Survey.SurveyAccessType.Public);

        if (activeOnly)
            query = query.Where(s => s.IsActive);

        if (status != null)
            query = query.Where(s => s.Status == status);

        //  :
        // -  StartsAt ,     StartsAt
        // -  EndsAt ,     EndsAt
        query = query.Where(s =>
            (s.StartsAt == null || s.StartsAt <= now) &&
            (s.EndsAt == null || s.EndsAt >= now));

        var surveys = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => ToSummary(s))
            .ToListAsync();

        return Ok(surveys);
    }

    // ========== GET: api/surveys/private/{inviteToken} ==========
    /// <summary>
    ///       (  ).
    ///
    ///        .
    /// </summary>
    [HttpGet("private/{inviteToken}")]
    [ProducesResponseType(typeof(Survey), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Survey>> GetPrivateSurveyByToken(string inviteToken)
    {
        if (string.IsNullOrWhiteSpace(inviteToken))
            return BadRequest("Invite token is required.");

        var survey = await _context.Surveys
            .Include(s => s.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s =>
                s.AccessType == Survey.SurveyAccessType.PrivateByLink &&
                s.InviteToken == inviteToken);

        if (survey == null)
            return NotFound("Survey not found.");

        DetachBackReferences(survey);
        return Ok(survey);
    }

    // ========== GET: api/surveys/byuser/5 ==========
    /// <summary>
    ///  ,    ( `CreatedBy`).
    /// </summary>
    [HttpGet("byuser/{userId:int}")]
    [ProducesResponseType(typeof(IEnumerable<SurveySummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SurveySummaryResponse>>> GetSurveysByUser(int userId)
    {
        var surveys = await _context.Surveys
            .Where(s => s.CreatedBy == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => ToSummary(s))
            .ToListAsync();

        return Ok(surveys);
    }

    // ========== GET: api/surveys/5 ==========
    /// <summary>
    ///    `Id` (,    ).
    ///
    /// :
    /// -      `GET /api/surveys/private/{inviteToken}`,
    ///       `Id`.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Survey), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Survey>> GetSurvey(int id)
    {
        var survey = await _context.Surveys
            .Include(s => s.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (survey == null)
            return NotFound($"Survey with ID {id} not found.");

        DetachBackReferences(survey);
        return Ok(survey);
    }

    // ========== POST: api/surveys/5/predictions ==========
    /// <summary>
    /// ??????? ??????? ??????? ?? ???????? (?????? ???????? ????? ????????).
    /// ?????????????? ?????? ??? ????????? ???????, ???????????? ?? ??????? (EndsAt ?????) ? ???????? ?? ???????.
    /// </summary>
    [HttpPost("{id:int}/predictions")]
    [ProducesResponseType(typeof(SurveyPredictionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SurveyPredictionResponse>> CreatePrediction(
        int id,
        [FromBody] CreateSurveyPredictionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var survey = await _context.Surveys
            .Include(s => s.Questions)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (survey == null)
            return NotFound($"Survey with ID {id} not found.");

        if (!survey.EnablePredictions)
            return BadRequest("Predictions are not enabled for this survey.");

        if (survey.AccessType != Survey.SurveyAccessType.Public || survey.EndsAt == null)
            return BadRequest("Predictions are supported only for public time-limited surveys.");

        if (survey.Status != Survey.SurveyStatus.Published)
            return BadRequest("Predictions are supported only for published surveys.");

        var now = DateTime.UtcNow;
        if (survey.StartsAt != null && survey.StartsAt.Value > now)
            return BadRequest("Survey is not started yet.");
        if (survey.EndsAt.Value <= now)
            return BadRequest("Survey is already finished.");

        if (request.Answers.Select(a => a.QuestionId).Distinct().Count() != request.Answers.Count)
            return BadRequest("Duplicate predictions for the same question are not allowed.");

        if (survey.Questions.Count == 0)
            return BadRequest("Survey has no questions.");

        if (request.Answers.Count != survey.Questions.Count)
            return BadRequest("Prediction must include exactly one leader for each question of the survey.");

        var exists = await _context.SurveyPredictions
            .AnyAsync(p => p.SurveyId == id && p.UserId == request.UserId, cancellationToken);
        if (exists)
            return Conflict("Prediction already exists for this user and survey.");

        var surveyQuestionIds = survey.Questions.Select(q => q.Id).ToHashSet();
        foreach (var a in request.Answers)
        {
            if (!surveyQuestionIds.Contains(a.QuestionId))
                return BadRequest($"Question {a.QuestionId} does not belong to survey {id}.");

            var question = survey.Questions.First(q => q.Id == a.QuestionId);
            var optionExists = question.Options.Any(o => o.Id == a.OptionId);
            if (!optionExists)
                return BadRequest($"Option {a.OptionId} does not belong to question {a.QuestionId}.");
        }

        var prediction = new SurveyPrediction
        {
            SurveyId = id,
            UserId = request.UserId,
            CreatedAt = now,
            Answers = request.Answers.Select(a => new SurveyPredictionAnswer
            {
                QuestionId = a.QuestionId,
                OptionId = a.OptionId
            }).ToList()
        };

        _context.SurveyPredictions.Add(prediction);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("Prediction already exists for this user and survey.");
        }

        return CreatedAtAction(nameof(GetPrediction), new { id, userId = request.UserId }, ToPredictionResponse(prediction));
    }

    // ========== GET: api/surveys/5/predictions/123 ==========
    [HttpGet("{id:int}/predictions/{userId:int}")]
    [ProducesResponseType(typeof(SurveyPredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SurveyPredictionResponse>> GetPrediction(int id, int userId, CancellationToken cancellationToken)
    {
        var prediction = await _context.SurveyPredictions
            .Include(p => p.Answers)
            .FirstOrDefaultAsync(p => p.SurveyId == id && p.UserId == userId, cancellationToken);

        if (prediction == null)
            return NotFound("Prediction not found.");

        return Ok(ToPredictionResponse(prediction));
    }

    // ========== POST: api/surveys/5/predictions/score ==========
    /// <summary>
    /// ?????????? ???????? ? ????????? ????? ?????????????. ????????????: ??? ??????????? ???????? ???????? ?? ???????????.
    /// </summary>
    [HttpPost("{id:int}/predictions/score")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> ScorePredictions(int id, CancellationToken cancellationToken)
    {
        var exists = await _context.Surveys.AnyAsync(s => s.Id == id, cancellationToken);
        if (!exists)
            return NotFound($"Survey with ID {id} not found.");

        try
        {
            var scoredCount = await _predictionScoringService.ScoreSurveyPredictionsAsync(id, cancellationToken);
            return Ok(new { SurveyId = id, ScoredPredictions = scoredCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ========== POST: api/surveys ==========
    /// <summary>
    ///   .
    ///
    ///  :
    /// - `CreatedAt`
    /// - `InviteToken` ( `AccessType=PrivateByLink`)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SurveySummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SurveySummaryResponse>> CreateSurvey([FromBody] CreateSurveyRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (request.EndsAt != null && request.StartsAt != null && request.EndsAt < request.StartsAt)
            return BadRequest("EndsAt cannot be earlier than StartsAt.");

        var now = DateTime.UtcNow;

        var survey = new Survey
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            CreatedAt = now, // explicit app timestamp to keep consistency across DBs
            UpdatedAt = null,
            CompletedAt = null,
            IsActive = true,
            Status = request.Status,
            AccessType = request.AccessType,
            IsAnonymous = request.IsAnonymous,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            CreatedBy = request.CreatedBy,
            EnablePredictions = request.EnablePredictions,
        };

        if (survey.AccessType == Survey.SurveyAccessType.PrivateByLink)
        {
            survey.InviteToken = await GenerateUniqueInviteTokenAsync();
        }

        if (request.Questions.Count > 0)
        {
            foreach (var q in request.Questions)
            {
                var question = new Question
                {
                    Text = q.Text.Trim(),
                    Order = q.Order,
                    IsRequired = q.IsRequired,
                    Options = q.Options.Select(o => new Option
                    {
                        Text = o.Text.Trim(),
                        Order = o.Order
                    }).ToList()
                };

                survey.Questions.Add(question);
            }
        }

        _context.Surveys.Add(survey);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, ToSummary(survey));
    }

    // ========== PUT: api/surveys/5 ==========
    /// <summary>
    ///    .
    ///
    /// :
    /// -  `UpdatedAt`
    /// -    `Closed`  `CompletedAt`,    
    /// -  `InviteToken`,     `PrivateByLink`    
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSurvey(int id, [FromBody] UpdateSurveyRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (id != request.Id)
            return BadRequest("ID in URL does not match request body.");

        if (request.EndsAt != null && request.StartsAt != null && request.EndsAt < request.StartsAt)
            return BadRequest("EndsAt cannot be earlier than StartsAt.");

        var survey = await _context.Surveys.FindAsync(id);
        if (survey == null)
            return NotFound($"Survey with ID {id} not found.");

        survey.Title = request.Title.Trim();
        survey.Description = request.Description;
        survey.IsActive = request.IsActive;
        survey.AccessType = request.AccessType;
        survey.IsAnonymous = request.IsAnonymous;
        survey.Status = request.Status;
        survey.StartsAt = request.StartsAt;
        survey.EndsAt = request.EndsAt;
        survey.CompletedAt = request.CompletedAt;
        survey.EnablePredictions = request.EnablePredictions;
        survey.UpdatedAt = DateTime.UtcNow;

        if (survey.AccessType == Survey.SurveyAccessType.PrivateByLink && string.IsNullOrWhiteSpace(survey.InviteToken))
        {
            survey.InviteToken = await GenerateUniqueInviteTokenAsync();
        }

        if (survey.Status == Survey.SurveyStatus.Closed && survey.CompletedAt == null)
        {
            survey.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ========== PATCH: api/surveys/5/status ==========
    /// <summary>
    ///  /  (  `IsActive`).
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSurveyActiveFlag(int id, [FromBody] bool isActive)
    {
        var survey = await _context.Surveys.FindAsync(id);
        if (survey == null)
            return NotFound($"Survey with ID {id} not found.");

        survey.IsActive = isActive;
        survey.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ========== DELETE: api/surveys/5 ==========
    /// <summary>
    ///   (hard-delete).
    ///
    /// -       .
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSurvey(int id)
    {
        var survey = await _context.Surveys.FindAsync(id);
        if (survey == null)
            return NotFound($"Survey with ID {id} not found.");

        _context.Surveys.Remove(survey);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ========== GET: api/surveys/stats ==========
    /// <summary>
    ///    .
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetStats()
    {
        var total = await _context.Surveys.CountAsync();
        var active = await _context.Surveys.CountAsync(s => s.IsActive);
        var publicCount = await _context.Surveys.CountAsync(s => s.AccessType == Survey.SurveyAccessType.Public);
        var privateCount = await _context.Surveys.CountAsync(s => s.AccessType == Survey.SurveyAccessType.PrivateByLink);

        return Ok(new
        {
            TotalSurveys = total,
            ActiveSurveys = active,
            PublicSurveys = publicCount,
            PrivateSurveys = privateCount
        });
    }

    private static SurveySummaryResponse ToSummary(Survey s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Description = s.Description,
        IsActive = s.IsActive,
        Status = s.Status,
        AccessType = s.AccessType,
        IsAnonymous = s.IsAnonymous,
        CreatedAt = s.CreatedAt,
        StartsAt = s.StartsAt,
        EndsAt = s.EndsAt,
        CompletedAt = s.CompletedAt
    };

    private static SurveyPredictionResponse ToPredictionResponse(SurveyPrediction p) => new()
    {
        Id = p.Id,
        SurveyId = p.SurveyId,
        UserId = p.UserId,
        CreatedAt = p.CreatedAt,
        IsScored = p.IsScored,
        Score = p.Score,
        ScoredAt = p.ScoredAt,
        Answers = p.Answers
            .OrderBy(a => a.QuestionId)
            .Select(a => new SurveyPredictionAnswerResponse
            {
                QuestionId = a.QuestionId,
                OptionId = a.OptionId
            })
            .ToList()
    };

    private async Task<string> GenerateUniqueInviteTokenAsync()
    {
        //  :
        // -  Guid   "N" (32 hex-  )
        // -     ,       
        for (var i = 0; i < 5; i++)
        {
            var token = Guid.NewGuid().ToString("N");
            var exists = await _context.Surveys.AnyAsync(s => s.InviteToken == token);
            if (!exists)
                return token;
        }

        //    .
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }

    private static void DetachBackReferences(Survey survey)
    {
        foreach (var q in survey.Questions)
        {
            q.Survey = null;
            foreach (var o in q.Options)
            {
                o.Question = null;
            }
        }
    }
}