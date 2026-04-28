using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ExamSelectionRuleConfiguration : IEntityTypeConfiguration<ExamSelectionRule>
{
    public void Configure(EntityTypeBuilder<ExamSelectionRule> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.QuestionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Count)
            .IsRequired();

        builder.HasOne(r => r.Exam)
            .WithMany(e => e.SelectionRules)
            .HasForeignKey(r => r.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
