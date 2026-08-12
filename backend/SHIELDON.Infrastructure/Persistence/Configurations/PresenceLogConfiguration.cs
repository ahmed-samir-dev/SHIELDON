using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for PresenceLog.
///
/// Each row represents one significant connectivity state change during a student's
/// exam attempt (Disconnected, Reconnected, PageRefreshed).
/// Heartbeats themselves are NOT stored as rows - only the state transitions are.
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

        // ── Event Details ────────────────────────────────────────────────────
        builder.Property(p => p.EventType)
            .IsRequired()
            .HasConversion<string>()  // Store as string for SSMS readability
            .HasMaxLength(30);

        builder.Property(p => p.OccurredAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── Relationships ────────────────────────────────────────────────────

        // PresenceLog → ExamAttempt
        builder.HasOne(p => p.Attempt)
            .WithMany(a => a.PresenceLogs)
            .HasForeignKey(p => p.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        // PresenceLog → User (student)
        builder.HasOne(p => p.Student)
            .WithMany()
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // PresenceLog → Exam (denormalized for timeline queries)
        builder.HasOne(p => p.Exam)
            .WithMany()
            .HasForeignKey(p => p.ExamId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ──────────────────────────────────────────────────────────
        builder.HasIndex(p => p.AttemptId);
        builder.HasIndex(p => new { p.AttemptId, p.OccurredAt });
    }
}
