using System.ComponentModel.DataAnnotations;
using SurveyService.Models;

namespace SurveyService.DTOs;

/// <summary>
/// DTO для создания опроса.
///
/// Почему DTO, а не `Survey` напрямую:
/// - Entity содержит служебные/вычисляемые поля (Id, CreatedAt, InviteToken, UpdatedAt),
///   которые мы не хотим принимать “как есть” от клиента.
/// - DTO помогает явно зафиксировать контракт API и проводить валидацию входных данных.
/// </summary>
public sealed class CreateSurveyRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public Survey.SurveyAccessType AccessType { get; set; } = Survey.SurveyAccessType.Public;

    public bool IsAnonymous { get; set; } = false;

    public Survey.SurveyStatus Status { get; set; } = Survey.SurveyStatus.Draft;

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool EnablePredictions { get; set; } = false;

    /// <summary>
    /// ID создателя опроса.
    ///
    /// Семантика:
    /// - 0 — создатель скрыт или это системное создание
    /// - >0 — ID пользователя
    /// </summary>
    public int CreatedBy { get; set; } = 0;

    /// <summary>
    /// Набор вопросов, которые создаются вместе с опросом.
    ///
    /// Если список пустой — опрос создастся без вопросов.
    /// </summary>
    public List<CreateQuestionRequest> Questions { get; set; } = new();
}

/// <summary>
/// DTO для обновления основных свойств опроса.
///
/// Замечание:
/// - В этом сервисе “редактирование вопросов/опций” не реализовано отдельными методами,
///   поэтому DTO обновляет только метаданные опроса и его состояние.
/// </summary>
public sealed class UpdateSurveyRequest
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public Survey.SurveyAccessType AccessType { get; set; } = Survey.SurveyAccessType.Public;

    public bool IsAnonymous { get; set; } = false;

    public Survey.SurveyStatus Status { get; set; } = Survey.SurveyStatus.Draft;

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool EnablePredictions { get; set; } = false;
}

/// <summary>
/// DTO для создания вопроса вместе с опросом.
/// </summary>
public sealed class CreateQuestionRequest
{
    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    [Range(1, 100)]
    public int Order { get; set; }

    public bool IsRequired { get; set; } = true;

    public List<CreateOptionRequest> Options { get; set; } = new();
}

/// <summary>
/// DTO для создания варианта ответа.
/// </summary>
public sealed class CreateOptionRequest
{
    [Required]
    [MaxLength(200)]
    public string Text { get; set; } = string.Empty;

    [Range(1, 50)]
    public int Order { get; set; }
}

/// <summary>
/// Короткий ответ для списков опросов (без вложенных вопросов/опций).
///
/// Используется в GET-эндпоинтах, где важна скорость и не нужно подтягивать весь граф.
/// </summary>
public sealed class SurveySummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Survey.SurveyStatus Status { get; set; }
    public Survey.SurveyAccessType AccessType { get; set; }
    public bool IsAnonymous { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class CreateSurveyPredictionRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [MinLength(1)]
    public List<CreateSurveyPredictionAnswerRequest> Answers { get; set; } = new();
}

public sealed class CreateSurveyPredictionAnswerRequest
{
    [Range(1, int.MaxValue)]
    public int QuestionId { get; set; }

    [Range(1, int.MaxValue)]
    public int OptionId { get; set; }
}

public sealed class SurveyPredictionResponse
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsScored { get; set; }
    public int Score { get; set; }
    public DateTime? ScoredAt { get; set; }
    public List<SurveyPredictionAnswerResponse> Answers { get; set; } = new();
}

public sealed class SurveyPredictionAnswerResponse
{
    public int QuestionId { get; set; }
    public int OptionId { get; set; }
}

