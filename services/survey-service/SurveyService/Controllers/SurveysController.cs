using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyService.Data;
using SurveyService.DTOs;
using SurveyService.Models;

namespace SurveyService.Controllers;

/// <summary>
/// API-контроллер для работы с опросами.
///
/// Роль контроллера:
/// - Принимает HTTP-запросы (REST) и возвращает ответы.
/// - Выполняет базовую валидацию входных данных (ModelState + простые проверки).
/// - Управляет жизненным циклом полей, которые должны задаваться сервером:
///   - `CreatedAt`, `UpdatedAt`, `InviteToken`, частично `CompletedAt`.
/// - Работает с БД через `AppDbContext` (EF Core).
///
/// Важно:
/// - Для списков возвращаем облегчённый DTO `SurveySummaryResponse` (без вопросов/опций),
///   чтобы не грузить весь граф сущностей и не замедлять выдачу.
/// - Для детального просмотра опроса (`GetSurvey` / `GetPrivateSurveyByToken`) возвращаем
///   Entity `Survey` с `Questions` и `Options`.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SurveysController> _logger;

    public SurveysController(AppDbContext context, ILogger<SurveysController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========== GET: api/surveys ==========
    /// <summary>
    /// Получить список всех опросов (без вложенных вопросов/опций).
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
    /// Получить список публичных опросов (доступных всем).
    ///
    /// Дополнительно:
    /// - Можно фильтровать по `status`
    /// - Можно ограничить только активными (`activeOnly=true`)
    /// - Учитываются временные окна `StartsAt`/`EndsAt` (если заданы)
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

        // Временное окно:
        // - если StartsAt задан, опрос показываем только после StartsAt
        // - если EndsAt задан, опрос показываем только до EndsAt
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
    /// Получить приватный опрос по токену доступа (опрос “по ссылке”).
    ///
    /// Возвращает полный объект опроса с вопросами и вариантами.
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

        return Ok(survey);
    }

    // ========== GET: api/surveys/byuser/5 ==========
    /// <summary>
    /// Получить опросы, созданные конкретным пользователем (по `CreatedBy`).
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
    /// Получить опрос по `Id` (детально, с вопросами и вариантами).
    ///
    /// Примечание:
    /// - Для приватных опросов корректнее использовать `GET /api/surveys/private/{inviteToken}`,
    ///   иначе клиент может “угадывать” `Id`.
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

        return Ok(survey);
    }

    // ========== POST: api/surveys ==========
    /// <summary>
    /// Создать новый опрос.
    ///
    /// Сервер задаёт:
    /// - `CreatedAt`
    /// - `InviteToken` (если `AccessType=PrivateByLink`)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Survey), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Survey>> CreateSurvey([FromBody] CreateSurveyRequest request)
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

        return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, survey);
    }

    // ========== PUT: api/surveys/5 ==========
    /// <summary>
    /// Обновить основные свойства опроса.
    ///
    /// Сервер:
    /// - обновляет `UpdatedAt`
    /// - при переводе в `Closed` ставит `CompletedAt`, если клиент не указал
    /// - генерирует `InviteToken`, если переводим опрос в `PrivateByLink` и токена ещё нет
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
    /// Быстро включить/выключить опрос (технический флаг `IsActive`).
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
    /// Удалить опрос (hard-delete).
    ///
    /// Из-за каскадных связей будут удалены вопросы и варианты.
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
    /// Простейшая статистика по опросам.
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

    private async Task<string> GenerateUniqueInviteTokenAsync()
    {
        // Генерация токена:
        // - используем Guid в формате "N" (32 hex-символа без дефисов)
        // - в БД стоит уникальный индекс, но на уровне кода дополнительно проверяем коллизии
        for (var i = 0; i < 5; i++)
        {
            var token = Guid.NewGuid().ToString("N");
            var exists = await _context.Surveys.AnyAsync(s => s.InviteToken == token);
            if (!exists)
                return token;
        }

        // Крайне маловероятный запасной вариант.
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}