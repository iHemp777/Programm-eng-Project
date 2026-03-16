using System.ComponentModel.DataAnnotations;

namespace SurveyService.Models;

/// <summary>
/// Класс, описывающий вариант ответа
/// </summary>
public class Option
{
    /// <summary>
    /// ID варианта
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Текст варианта
    /// </summary>
    [Required(ErrorMessage = "Текст варианта обязателен")]
    [MaxLength(200, ErrorMessage = "Текст варианта не может быть длиннее 200 символов")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Порядок варианта
    /// </summary>
    [Range(1, 50, ErrorMessage = "Порядок должен быть от 1 до 50")]
    public int Order { get; set; }

    /// <summary>
    /// ID вопроса
    /// </summary>
    public int QuestionId { get; set; }

    /// <summary>
    /// Ссылка на вопрос
    /// </summary>
    public Question? Question { get; set; }
}