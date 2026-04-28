using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for ExamQuestion — the centralized course question bank.
/// Questions are now course-scoped (CourseId FK) and can be reused across exams.
/// </summary>
public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.QuestionText)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Points)
            .HasPrecision(5, 2);

        // FK → Course (centralized bank, not exam-scoped)
        builder.HasOne(e => e.Course)
            .WithMany(c => c.QuestionBankItems)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Options cascade with the question
        builder.HasMany(e => e.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
