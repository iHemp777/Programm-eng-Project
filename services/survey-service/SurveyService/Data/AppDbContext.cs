using Microsoft.EntityFrameworkCore;
using SurveyService.Models;

namespace SurveyService.Data;

/// <summary>
/// Класс, который отвечает за связь с базой данных
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Конструктор - принимает настройки подключения
    /// </summary>
  
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Таблица Surveys в БД
    /// </summary>
    public DbSet<Survey> Surveys { get; set; }

    /// <summary>
    /// Таблица Questions в БД
    /// </summary>
    public DbSet<Question> Questions { get; set; }

    /// <summary>
    /// Таблица Options в БД
    /// </summary>
    public DbSet<Option> Options { get; set; }

    /// <summary>
    /// Настройка связей между таблицами
    /// Вызывается автоматически при создании БД
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Настройка связи: Опрос → Вопросы (один ко многим)
        modelBuilder.Entity<Survey>()
            .HasMany(s => s.Questions)           // У опроса много вопросов
            .WithOne(q => q.Survey)              // У вопроса один опрос
            .HasForeignKey(q => q.SurveyId)      // Внешний ключ в таблице Question
            .OnDelete(DeleteBehavior.Cascade);   // При удалении опроса удаляются и вопросы

        // Настройка связи: Вопрос → Варианты (один ко многим)
        modelBuilder.Entity<Question>()
            .HasMany(q => q.Options)              // У вопроса много вариантов
            .WithOne(o => o.Question)             // У варианта один вопрос
            .HasForeignKey(o => o.QuestionId)     // Внешний ключ в таблице Option
            .OnDelete(DeleteBehavior.Cascade);    // При удалении вопроса удаляются и варианты

        // Индекс для быстрого поиска по дате
        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_Surveys_CreatedAt");

        // Индекс для поиска активных опросов
        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_Surveys_IsActive");

        // Индексы для новых полей
        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.TimeType)
            .HasDatabaseName("IX_Surveys_TimeType");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.AccessType)
            .HasDatabaseName("IX_Surveys_AccessType");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.CreatedBy)
            .HasDatabaseName("IX_Surveys_CreatedBy");

        // Составной индекс для частых запросов
        modelBuilder.Entity<Survey>()
            .HasIndex(s => new { s.IsActive, s.AccessType })
            .HasDatabaseName("IX_Surveys_IsActive_AccessType");

        // Устанавливаем значения по умолчанию
        modelBuilder.Entity<Survey>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Survey>()
            .Property(s => s.IsActive)
            .HasDefaultValue(true);

        // Значения по умолчанию для новых полей
        modelBuilder.Entity<Survey>()
            .Property(s => s.TimeType)
            .HasConversion<int>()
            .HasDefaultValue(SurveyTimeType.NoTimeLimit);

        modelBuilder.Entity<Survey>()
            .Property(s => s.QuestionType)
            .HasConversion<int>()
            .HasDefaultValue(SurveyQuestionType.MultipleQuestions);

        modelBuilder.Entity<Survey>()
            .Property(s => s.AccessType)
            .HasConversion<int>()
            .HasDefaultValue(SurveyAccessType.PublicNotAnonymous);

        modelBuilder.Entity<Survey>()
            .Property(s => s.CreatedBy)
            .HasDefaultValue(0);

        // CompletedAt может быть null
        modelBuilder.Entity<Survey>()
            .Property(s => s.CompletedAt)
            .IsRequired(false);
    }
}