using Microsoft.EntityFrameworkCore;
using SurveyService.Models;

namespace SurveyService.Data;

/// <summary>
/// EF Core DbContext — слой доступа к данным.
///
/// Отвечает за:
/// - Сопоставление Entity-классов (`Survey`, `Question`, `Option`) с таблицами БД.
/// - Настройку связей, каскадных удалений, индексов и значений по умолчанию.
///
/// Примечание по удалению:
/// - В проекте выбран hard-delete: удаляем записи физически.
/// - Каскад настроен так, что при удалении опроса удалятся связанные вопросы и варианты.
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
    /// Настройка модели EF (таблицы/связи/индексы/дефолты).
    /// Вызывается EF автоматически при инициализации модели.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Связь Survey (1) → Question (many)
        modelBuilder.Entity<Survey>()
            .HasMany(s => s.Questions)           // У опроса много вопросов
            .WithOne(q => q.Survey)              // У вопроса один опрос
            .HasForeignKey(q => q.SurveyId)      // Внешний ключ в таблице Question
            .OnDelete(DeleteBehavior.Cascade);   // При удалении опроса удаляются и вопросы

        // Связь Question (1) → Option (many)
        modelBuilder.Entity<Question>()
            .HasMany(q => q.Options)              // У вопроса много вариантов
            .WithOne(o => o.Question)             // У варианта один вопрос
            .HasForeignKey(o => o.QuestionId)     // Внешний ключ в таблице Option
            .OnDelete(DeleteBehavior.Cascade);    // При удалении вопроса удаляются и варианты

        // Индексы для типичных запросов (списки/фильтры)
        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_Surveys_CreatedAt");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_Surveys_IsActive");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.CreatedBy)
            .HasDatabaseName("IX_Surveys_CreatedBy");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.Status)
            .HasDatabaseName("IX_Surveys_Status");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.AccessType)
            .HasDatabaseName("IX_Surveys_AccessType");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.IsAnonymous)
            .HasDatabaseName("IX_Surveys_IsAnonymous");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => new { s.IsActive, s.Status })
            .HasDatabaseName("IX_Surveys_IsActive_Status");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => new { s.IsActive, s.AccessType })
            .HasDatabaseName("IX_Surveys_IsActive_AccessType");

        modelBuilder.Entity<Survey>()
            .HasIndex(s => s.InviteToken)
            .IsUnique()
            .HasDatabaseName("UX_Surveys_InviteToken");

        // Значения по умолчанию (в БД)
        modelBuilder.Entity<Survey>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Survey>()
            .Property(s => s.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Survey>()
            .Property(s => s.Status)
            .HasConversion<int>()
            .HasDefaultValue(Survey.SurveyStatus.Draft);

        modelBuilder.Entity<Survey>()
            .Property(s => s.AccessType)
            .HasConversion<int>()
            .HasDefaultValue(Survey.SurveyAccessType.Public);

        modelBuilder.Entity<Survey>()
            .Property(s => s.IsAnonymous)
            .HasDefaultValue(false);

        modelBuilder.Entity<Survey>()
            .Property(s => s.CreatedBy)
            .HasDefaultValue(0);

        // Nullable поля (по умолчанию EF сам определит, но фиксируем намерение явно)
        modelBuilder.Entity<Survey>()
            .Property(s => s.CompletedAt)
            .IsRequired(false);

        modelBuilder.Entity<Survey>()
            .Property(s => s.StartsAt)
            .IsRequired(false);

        modelBuilder.Entity<Survey>()
            .Property(s => s.EndsAt)
            .IsRequired(false);

        modelBuilder.Entity<Survey>()
            .Property(s => s.UpdatedAt)
            .IsRequired(false);

        modelBuilder.Entity<Survey>()
            .Property(s => s.InviteToken)
            .IsRequired(false);
    }
}