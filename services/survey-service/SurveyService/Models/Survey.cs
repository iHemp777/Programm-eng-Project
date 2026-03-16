using System.ComponentModel.DataAnnotations;

namespace SurveyService.Models;

/// <summary>
///  ласс, описывающий опрос
/// </summary>
public class Survey
{
    /// <summary>
    /// ID опроса (первичный ключ)
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Ќазвание опроса
    /// </summary>
    [Required(ErrorMessage = "Ќазвание опроса об€зательно")]
    [MaxLength(200, ErrorMessage = "Ќазвание не может быть длиннее 200 символов")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// ќписание опроса (необ€зательное)
    /// </summary>
    [MaxLength(1000, ErrorMessage = "ќписание не может быть длиннее 1000 символов")]
    public string? Description { get; set; }

    /// <summary>
    /// ƒата создани€
    /// </summary>
    public DateTime CreatedAt { get; set; }
    public DateTime CompletedAt { get; set; }

    public enum SurveyTimeType
    {
        Time1 = 0,//безвременный, результаты нельз€ предсказать
        Time2 = 1,//временный, пользователь может предсказать результат, однако увидеть сможет только после истечени€ времени
    }

    public enum SurveyType
    {
        Type1 = 0,//1 вопрос
        Type2 = 1,//более 1 вопроса
    }

    public enum SurveyAccessType
    {
        publicNotAnonymousSurvey = 0,//пользователи вид€т кто глосовал
        publicAnonymousSurvey = 1,//ползователи не вид€т кто голосовал
        privateNotAnonymousSurvey = 2,//доступен только по ссылке, но автор видит кто и как голосовал
        privateAnonymousSurvey = 3//доступен только по ссылке, но автор не видит кто и как голосовал
    }

    /// <summary>
    /// јктивен ли опрос
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// —в€зь с вопросами (один опрос может иметь много вопросов)
    /// </summary>
    public List<Question> Questions { get; set; } = new();

    //ID создател€ опроса
    public int? CreatedBy { get; set; } //может быть null, значит польователь-создатель скрыл себ€
}