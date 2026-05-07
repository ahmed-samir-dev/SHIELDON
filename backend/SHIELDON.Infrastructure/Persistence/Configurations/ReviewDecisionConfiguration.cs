using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for ReviewDecision.
///
/// One ReviewDecision per attempt at most (enforced by unique index on AttemptId).
/// Cascade restrict on both FKs — review decisions are permanent audit records.
/// </summary>
public class ReviewDecisionConfiguration : IEntityTypeConfiguration<ReviewDecision>
{
    public void Configure(EntityTypeBuilder<ReviewDecision> builder)
    {
        builder.ToTable("ReviewDecisions");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Foreign Keys ────────────────────────────────────────────────────
        builder.Property(r => r.AttemptId).IsRequired();
        builder.Property(r => r.ReviewerId).IsRequired();

        // ── Decision Details ─────────────────────────────────────────────────
        builder.Property(r => r.Decision)
            .IsRequired()
            .HasConversion<string>()   // Store as readable string in SSMS
            .HasMaxLength(30);

        builder.Property(r => r.Notes)
            .HasMaxLength(2000);

        builder.Property(r => r.ReviewedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── Relationships ────────────────────────────────────────────────────

        // ReviewDecision → ExamAttempt (exactly one decision per attempt)
        builder.HasOne(r => r.Attempt)
            .WithOne(a => a.ReviewDecision)
            .HasForeignKey<ReviewDecision>(r => r.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        // ReviewDecision → User (reviewer: tutor or admin)
        builder.HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes ───────────────────────────────────────────────────────────
        // Unique: one review decision per attempt
        builder.HasIndex(r => r.AttemptId).IsUnique();
    }
}
