using Microsoft.EntityFrameworkCore;
using VotingService.Models;

namespace VotingService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Vote> Votes { get; set; }
    public DbSet<VoteAnswer> VoteAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vote>()
            .HasMany(v => v.Answers)
            .WithOne(a => a.Vote)
            .HasForeignKey(a => a.VoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vote>()
            .HasIndex(v => new { v.SurveyId, v.VoterId })
            .IsUnique()
            .HasDatabaseName("UX_Votes_SurveyId_VoterId");

        modelBuilder.Entity<Vote>()
            .HasIndex(v => v.SurveyId)
            .HasDatabaseName("IX_Votes_SurveyId");

        modelBuilder.Entity<VoteAnswer>()
            .HasIndex(a => new { a.QuestionId, a.OptionId })
            .HasDatabaseName("IX_VoteAnswers_QuestionId_OptionId");

        modelBuilder.Entity<Vote>()
            .Property(v => v.VotedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
