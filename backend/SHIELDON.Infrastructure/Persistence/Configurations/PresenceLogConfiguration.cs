using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for PresenceLog.
///
/// Each row is a single lifecycle event during a student's exam attempt
/// (heartbeat, disconnect, reconnect, tutor termination, etc.).
/// Denormalized ExamId and CourseId allow efficient tutor dashboard queries.
/// </summary>
public class PresenceLogConfiguration : IEntityTypeConfiguration<PresenceLog>
{
    public void Configure(EntityTypeBuilder<PresenceLog> builder)
    {
        builder.ToTable("PresenceLogs");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Foreign Keys ────────────────────────────────────────────────────
        builder.Property(p => p.AttemptId).IsRequired();
        builder.Property(p => p.StudentId).IsRequired();
        builder.Property(p => p.ExamId).IsRequired();
        builder.Property(p => p.CourseId).IsRequired();

        // ── Event Details ────────────────────────────────────────────────────
        builder.Property(p => p.EventType)
            .IsRequired()
            .HasConversion<string>()   // Stored as readable string in SSMS
            .HasMaxLength(30);

        builder.Property(p => p.Detail)
            .HasMaxLength(500);

        builder.Property(p => p.OccurredAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── Relationships ────────────────────────────────────────────────────

        // PresenceLog → ExamAttempt (many presence events per attempt)
        // Restrict delete: presence logs are audit records — never auto-deleted
        builder.HasOne(p => p.Attempt)
            .WithMany(a => a.PresenceLogs)
            .HasForeignKey(p => p.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        // PresenceLog → User (student)
        builder.HasOne(p => p.Student)
            .WithMany()
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // PresenceLog → Exam (denormalized)
        builder.HasOne(p => p.Exam)
            .WithMany()
            .HasForeignKey(p => p.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        // PresenceLog → Course (denormalized)
        builder.HasOne(p => p.Course)
            .WithMany()
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ───────────────────────────────────────────────────────────
        // Primary access pattern: "all events for this attempt" (timeline view)
        builder.HasIndex(p => p.AttemptId);
        // Secondary: "all events for this exam" (exam-level dashboard)
        builder.HasIndex(p => p.ExamId);
        // Background service query: "stale active attempts by exam"
        builder.HasIndex(p => new { p.ExamId, p.EventType });
    }
}
