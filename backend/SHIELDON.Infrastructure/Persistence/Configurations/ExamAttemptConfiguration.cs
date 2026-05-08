using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Score)
            .HasPrecision(5, 2);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Answers)
            .WithOne(a => a.Attempt)
            .HasForeignKey(a => a.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Token)
            .WithOne(t => t.Attempt)
            .HasForeignKey<ExamToken>(t => t.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes for dashboard queries
        builder.HasIndex(e => e.ExamId);
        builder.HasIndex(e => e.StudentId);
        builder.HasIndex(e => new { e.ExamId, e.Status });
    }
}
