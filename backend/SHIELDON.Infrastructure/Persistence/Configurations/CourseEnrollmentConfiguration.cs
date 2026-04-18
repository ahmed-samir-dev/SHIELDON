using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for CourseEnrollments.
/// Enforces: one enrollment record per student+course combination,
/// FK constraints with Restrict delete to preserve audit history.
/// </summary>
public class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.ToTable("CourseEnrollments");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Status ──────────────────────────────────────────────
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // ── Rejection Tracking ──────────────────────────────────
        builder.Property(e => e.RejectionCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.CooldownUntil)
            .HasColumnType("DATETIME2");

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        // ── Review Info ─────────────────────────────────────────
        builder.Property(e => e.ReviewedAt)
            .HasColumnType("DATETIME2");

        // ── Timestamps ──────────────────────────────────────────
        builder.Property(e => e.RequestedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── Unique Index: one enrollment record per student per course ──
        // This prevents duplicate enrollment rows for the same pair.
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique()
            .HasDatabaseName("IX_CourseEnrollments_StudentId_CourseId");

        // ── FK: Student ─────────────────────────────────────────
        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: Course ──────────────────────────────────────────
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: ReviewedBy (Admin/Tutor who approved/rejected) ──
        builder.HasOne(e => e.ReviewedBy)
            .WithMany()
            .HasForeignKey(e => e.ReviewedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
