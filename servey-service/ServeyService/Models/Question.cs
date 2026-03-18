using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SurveyService.Models;

/// <summary>
/// Класс, описывающий вопрос
/// </summary>
public class Question
{
    /// <summary>
    /// ID вопроса
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Текст вопроса
    /// </summary>
    [Required(ErrorMessage = "Текст вопроса обязателен")]
    [MaxLength(500, ErrorMessage = "Текст вопроса не может быть длиннее 500 символов")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Порядок вопроса (1, 2, 3...)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Порядок должен быть от 1 до 100")]
    public int Order { get; set; }

    /// <summary>
    /// Обязательный ли вопрос
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// ID опроса, к которому относится вопрос
    /// </summary>
    public int SurveyId { get; set; }

    /// <summary>
    /// Ссылка на опрос (навигационное свойство)
    /// </summary>
    [JsonIgnore]
    public Survey? Survey { get; set; }

    /// <summary>
    /// Варианты ответов на этот вопрос
    /// </summary>
    public List<Option> Options { get; set; } = new();
}