using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurveyService.Data;
using SurveyService.Models;

namespace SurveyService.Controllers;

/// <summary>
/// Контроллер для работы с опросами
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SurveysController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SurveysController> _logger;

    /// <summary>
    /// Конструктор контроллера
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер для записи событий</param>
    public SurveysController(AppDbContext context, ILogger<SurveysController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========== GET: api/surveys ==========
    /// <summary>
    /// Получить все опросы
    /// </summary>
    /// <returns>Список всех опросов</returns>
    /// <response code="200">Успешно получены</response>
    /// <response code="500">Ошибка сервера</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Survey>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Survey>>> GetSurveys()
    {
        try
        {
            _logger.LogInformation("Начало получения всех опросов");

            var surveys = await _context.Surveys
                .Include(s => s.Questions)           // Загружаем вопросы
                    .ThenInclude(q => q.Options)     // Загружаем варианты ответов
                .OrderByDescending(s => s.CreatedAt) // Сначала новые
                .ToListAsync();

            _logger.LogInformation("Успешно получено {Count} опросов", surveys.Count);
            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении всех опросов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== GET: api/surveys/active ==========
    /// <summary>
    /// Получить только активные опросы
    /// </summary>
    /// <returns>Список активных опросов</returns>
    /// <response code="200">Успешно получены</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<Survey>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Survey>>> GetActiveSurveys()
    {
        try
        {
            _logger.LogInformation("Получение активных опросов");

            var surveys = await _context.Surveys
                .Where(s => s.IsActive)              // Только активные
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении активных опросов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== GET: api/surveys/public ==========
    /// <summary>
    /// Получить публичные опросы (доступные всем)
    /// </summary>
    /// <returns>Список публичных опросов</returns>
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<Survey>>> GetPublicSurveys()
    {
        try
        {
            _logger.LogInformation("Получение публичных опросов");

            var surveys = await _context.Surveys
                .Where(s => s.AccessType == SurveyAccessType.PublicNotAnonymous ||
                           s.AccessType == SurveyAccessType.PublicAnonymous)
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении публичных опросов");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== GET: api/surveys/byuser/5 ==========
    /// <summary>
    /// Получить опросы созданные конкретным пользователем
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns>Опросы пользователя</returns>
    [HttpGet("byuser/{userId}")]
    public async Task<ActionResult<IEnumerable<Survey>>> GetSurveysByUser(int userId)
    {
        try
        {
            _logger.LogInformation("Получение опросов пользователя {UserId}", userId);

            var surveys = await _context.Surveys
                .Where(s => s.CreatedBy == userId)
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(surveys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении опросов пользователя {UserId}", userId);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== GET: api/surveys/5 ==========
    /// <summary>
    /// Получить опрос по ID
    /// </summary>
    /// <param name="id">ID опроса</param>
    /// <returns>Опрос с указанным ID</returns>
    /// <response code="200">Опрос найден</response>
    /// <response code="404">Опрос не найден</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Survey), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Survey>> GetSurvey(int id)
    {
        try
        {
            _logger.LogInformation("Поиск опроса с ID {Id}", id);

            var survey = await _context.Surveys
                .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (survey == null)
            {
                _logger.LogWarning("Опрос с ID {Id} не найден", id);
                return NotFound($"Опрос с ID {id} не найден");
            }

            _logger.LogInformation("Опрос с ID {Id} успешно найден", id);
            return Ok(survey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении опроса с ID {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== POST: api/surveys ==========
    /// <summary>
    /// Создать новый опрос
    /// </summary>
    /// <param name="survey">Данные нового опроса</param>
    /// <returns>Созданный опрос</returns>
    /// <response code="201">Опрос успешно создан</response>
    /// <response code="400">Неверные данные</response>
    [HttpPost]
    [ProducesResponseType(typeof(Survey), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Survey>> CreateSurvey(Survey survey)
    {
        try
        {
            _logger.LogInformation("Начало создания нового опроса с названием {Title}", survey.Title);

            // Валидация
            if (survey == null)
                return BadRequest("Данные опроса не предоставлены");

            if (string.IsNullOrWhiteSpace(survey.Title))
                return BadRequest("Название опроса обязательно");

            // Устанавливаем значения по умолчанию
            survey.CreatedAt = DateTime.UtcNow;

            // Если CreatedBy не указан, ставим 0 (система)
            if (survey.CreatedBy == 0)
                survey.CreatedBy = 0;

            // Сохраняем в БД
            _context.Surveys.Add(survey);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Опрос успешно создан с ID {Id}", survey.Id);

            // Возвращаем созданный объект с ссылкой на его получение
            return CreatedAtAction(nameof(GetSurvey), new { id = survey.Id }, survey);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Ошибка базы данных при создании опроса");
            return StatusCode(500, "Ошибка при сохранении в базу данных");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании опроса");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== PUT: api/surveys/5 ==========
    /// <summary>
    /// Обновить существующий опрос
    /// </summary>
    /// <param name="id">ID опроса для обновления</param>
    /// <param name="survey">Новые данные опроса</param>
    /// <returns>Статус операции</returns>
    /// <response code="204">Успешно обновлено</response>
    /// <response code="400">ID не совпадает</response>
    /// <response code="404">Опрос не найден</response>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSurvey(int id, Survey survey)
    {
        try
        {
            _logger.LogInformation("Обновление опроса с ID {Id}", id);

            // Проверка ID
            if (id != survey.Id)
            {
                return BadRequest("ID в URL не совпадает с ID опроса");
            }

            // Проверка существования
            var existingSurvey = await _context.Surveys.FindAsync(id);
            if (existingSurvey == null)
            {
                return NotFound($"Опрос с ID {id} не найден");
            }

            // Обновляем поля (ТОЛЬКО разрешенные)
            existingSurvey.Title = survey.Title;
            existingSurvey.Description = survey.Description;
            existingSurvey.IsActive = survey.IsActive;
            existingSurvey.TimeType = survey.TimeType;
            existingSurvey.QuestionType = survey.QuestionType;
            existingSurvey.AccessType = survey.AccessType;
            existingSurvey.CompletedAt = survey.CompletedAt;
            // CreatedAt НЕ меняем - дата создания должна остаться
            // CreatedBy НЕ меняем - автора не меняем

            // Помечаем как измененный
            _context.Entry(existingSurvey).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Опрос с ID {Id} успешно обновлен", id);
            return NoContent();
        }
        catch (DbUpdateConcurrencyException conEx)
        {
            _logger.LogError(conEx, "Конфликт при обновлении опроса {Id}", id);
            return StatusCode(409, "Данные были изменены другим пользователем");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обновлении опроса {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== PATCH: api/surveys/5/status ==========
    /// <summary>
    /// Изменить статус опроса (активен/неактивен)
    /// </summary>
    /// <param name="id">ID опроса</param>
    /// <param name="isActive">Новый статус</param>
    /// <returns>Статус операции</returns>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateSurveyStatus(int id, [FromBody] bool isActive)
    {
        try
        {
            _logger.LogInformation("Изменение статуса опроса {Id} на {Status}", id, isActive);

            var survey = await _context.Surveys.FindAsync(id);
            if (survey == null)
            {
                return NotFound($"Опрос с ID {id} не найден");
            }

            survey.IsActive = isActive;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при изменении статуса опроса {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== DELETE: api/surveys/5 ==========
    /// <summary>
    /// Удалить опрос
    /// </summary>
    /// <param name="id">ID опроса для удаления</param>
    /// <returns>Статус операции</returns>
    /// <response code="204">Успешно удалено</response>
    /// <response code="404">Опрос не найден</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSurvey(int id)
    {
        try
        {
            _logger.LogInformation("Удаление опроса с ID {Id}", id);

            var survey = await _context.Surveys.FindAsync(id);
            if (survey == null)
            {
                return NotFound($"Опрос с ID {id} не найден");
            }

            _context.Surveys.Remove(survey);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Опрос с ID {Id} успешно удален", id);
            return NoContent();
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Ошибка БД при удалении опроса {Id}", id);
            return StatusCode(500, "Ошибка при удалении из базы данных");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при удалении опроса {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    // ========== GET: api/surveys/stats ==========
    /// <summary>
    /// Получить статистику по опросам
    /// </summary>
    /// <returns>Статистика</returns>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        try
        {
            var total = await _context.Surveys.CountAsync();
            var active = await _context.Surveys.CountAsync(s => s.IsActive);
            var public_ = await _context.Surveys.CountAsync(s =>
                s.AccessType == SurveyAccessType.PublicNotAnonymous ||
                s.AccessType == SurveyAccessType.PublicAnonymous);
            var private_ = await _context.Surveys.CountAsync(s =>
                s.AccessType == SurveyAccessType.PrivateNotAnonymous ||
                s.AccessType == SurveyAccessType.PrivateAnonymous);

            return Ok(new
            {
                TotalSurveys = total,
                ActiveSurveys = active,
                PublicSurveys = public_,
                PrivateSurveys = private_
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении статистики");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}