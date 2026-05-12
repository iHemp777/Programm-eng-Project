using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SurveyService.Models;

/// <summary>
/// Основная доменная модель опроса.
///
/// Важно:
/// - Этот класс является EF Core Entity (таблица `Surveys`).
/// - Связи:
///   - `Survey` (1) ? `Question` (many) через `Survey.Questions` и `Question.SurveyId`
///   - `Question` (1) ? `Option` (many) через `Question.Options` и `Option.QuestionId`
/// - Доступ к опросу определяется комбинацией `AccessType` и (для приватных) `InviteToken`.
/// - Жизненный цикл описывается `Status`, а актуальность — `IsActive`.
/// </summary>
public class Survey
{
    /// <summary>
    /// ID опроса (первичный ключ).
    ///
    /// Так как вы выбрали вариант B (внешние ссылки по `Id:int`), именно это поле
    /// используется во внешнем API как идентификатор опроса.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Название опроса.
    /// </summary>
    [Required(ErrorMessage = "  ")]
    [MaxLength(200, ErrorMessage = "     200 ")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Описание опроса (необязательное).
    /// </summary>
    [MaxLength(1000, ErrorMessage = "     1000 ")]
    public string? Description { get; set; }

    /// <summary>
    /// Дата и время создания (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Дата и время завершения (UTC).
    ///
    /// Может быть null, если опрос ещё не завершён/не закрыт.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Статус жизненного цикла опроса.
    ///
    /// Рекомендуемая логика:
    /// - Draft: черновик (может быть недоступен для общего списка)
    /// - Published: опубликован
    /// - Closed: закрыт (обычно фиксируем `CompletedAt`)
    /// - Archived: архив (технический/исторический статус)
    /// </summary>
    public enum SurveyStatus
    {
        Draft = 0,
        Published = 1,
        Closed = 2,
        Archived = 3
    }

    /// <summary>
    /// Тип по времени (исторически/на будущее).
    ///
    /// Сейчас сервис использует явные окна `StartsAt`/`EndsAt`. Enum оставлен как
    /// доменная подсказка/возможность расширения.
    /// </summary>
    public enum SurveyTimeType
    {
        Time1 = 0,//,   
        Time2 = 1,//,    ,       
    }

    /// <summary>
    /// Тип по количеству вопросов (исторически/на будущее).
    ///
    /// Сейчас количество вопросов определяется содержимым `Questions`.
    /// </summary>
    public enum SurveyType
    {
        Type1 = 0,//1 
        Type2 = 1,// 1 
    }

    /// <summary>
    /// Тип доступа к опросу.
    ///
    /// - Public: доступен всем (по `Id`)
    /// - PrivateByLink: доступен только по `InviteToken` (см. `SurveysController.GetPrivateSurveyByToken`)
    /// </summary>
    public enum SurveyAccessType
    {
        Public = 0,
        PrivateByLink = 1
    }

    /// <summary>
    /// Текущий статус опроса.
    /// </summary>
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;

    /// <summary>
    /// Тип доступа (публичный или приватный по ссылке).
    /// </summary>
    public SurveyAccessType AccessType { get; set; } = SurveyAccessType.Public;

    /// <summary>
    /// Признак анонимности.
    ///
    /// Если true — потребителям API/клиенту следует скрывать автора/голоса
    /// (в зависимости от того, как будет реализована подсистема голосования).
    /// </summary>
    public bool IsAnonymous { get; set; } = false;

    /// <summary>
    /// Когда опрос становится доступным (UTC, необязательно).
    ///
    /// Если null — доступен сразу (при условии других ограничений).
    /// </summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>
    /// Когда опрос заканчивается (UTC, необязательно).
    ///
    /// Если null — не имеет конца по времени.
    /// </summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>
    /// Токен для приватных опросов, доступных по ссылке.
    ///
    /// - Для `AccessType=Public` должен быть null.
    /// - Для `AccessType=PrivateByLink` генерируется на сервере.
    /// - В `AppDbContext` настроен уникальный индекс, чтобы токены не повторялись.
    /// </summary>
    [MaxLength(64)]
    public string? InviteToken { get; set; }

    /// <summary>
    /// Дата и время последнего изменения (UTC, необязательно).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Активен ли опрос.
    ///
    /// Это технический флаг “включён/выключён”, не равен статусу жизненного цикла (`Status`).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Вопросы опроса (связь 1:N).
    ///
    /// При удалении опроса вопросы удаляются каскадно (см. `AppDbContext`).
    /// </summary>
    [JsonIgnore]
    public List<Question> Questions { get; set; } = new();

    /// <summary>
    /// ID создателя опроса.
    ///
    /// Семантика:
    /// - 0 — создатель скрыт (анонимно) или опрос создан системой
    /// - >0 — ID пользователя-создателя
    /// </summary>
    public int CreatedBy { get; set; } = 0;

    /// <summary>
    /// Разрешены ли прогнозы лидеров по вопросам для этого опроса.
    ///
    /// Если false — эндпоинты прогнозов должны отклонять создание прогнозов.
    /// </summary>
    public bool EnablePredictions { get; set; } = false;
}