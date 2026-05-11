using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for ViolationLog.
///
/// Each row represents one anti-cheat violation event during a student's exam attempt.
/// Denormalized ExamId and CourseId are stored for efficient querying in the monitoring
/// dashboard without needing joins through ExamAttempt every time.
/// </summary>
public class ViolationLogConfiguration : IEntityTypeConfiguration<ViolationLog>
{
    public void Configure(EntityTypeBuilder<ViolationLog> builder)
    {
        builder.ToTable("ViolationLogs");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Foreign Keys ────────────────────────────────────────────────────
        builder.Property(v => v.AttemptId).IsRequired();
        builder.Property(v => v.StudentId).IsRequired();
        builder.Property(v => v.ExamId).IsRequired();
        builder.Property(v => v.CourseId).IsRequired();

        // ── Violation Details ────────────────────────────────────────────────
        builder.Property(v => v.Type)
            .IsRequired()
            .HasConversion<string>()   // Store as string for readability in SSMS
            .HasMaxLength(50);

        builder.Property(v => v.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.OccurredAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(v => v.WasAutoSubmit)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── Relationships ────────────────────────────────────────────────────

        // ViolationLog → ExamAttempt (many violations per attempt)
        // No cascade delete from attempt — violations are audit records, keep them even if attempt is deleted
        builder.HasOne(v => v.Attempt)
            .WithMany()
            .HasForeignKey(v => v.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        // ViolationLog → User (student)
        builder.HasOne(v => v.Student)
            .WithMany()
            .HasForeignKey(v => v.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ViolationLog → Exam (denormalized for dashboard queries)
        builder.HasOne(v => v.Exam)
            .WithMany()
            .HasForeignKey(v => v.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        // ViolationLog → Course (denormalized for dashboard queries)
        builder.HasOne(v => v.Course)
            .WithMany()
            .HasForeignKey(v => v.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes for Dashboard Queries ─────────────────────────────────────
        // Tutors commonly query: "all violations for this exam" or "this attempt"
        builder.HasIndex(v => v.AttemptId);
        builder.HasIndex(v => v.ExamId);
        builder.HasIndex(v => new { v.ExamId, v.StudentId });
    }
}
