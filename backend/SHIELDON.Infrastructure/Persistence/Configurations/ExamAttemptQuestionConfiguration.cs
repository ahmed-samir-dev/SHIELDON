using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ExamAttemptQuestionConfiguration : IEntityTypeConfiguration<ExamAttemptQuestion>
{
    public void Configure(EntityTypeBuilder<ExamAttemptQuestion> builder)
    {
        builder.HasKey(aq => aq.Id);

        builder.HasOne(aq => aq.Attempt)
            .WithMany(a => a.AttemptQuestions)
            .HasForeignKey(aq => aq.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // No cascade on question deletion — keep the snapshot intact for grading history
        builder.HasOne(aq => aq.Question)
            .WithMany()
            .HasForeignKey(aq => aq.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
