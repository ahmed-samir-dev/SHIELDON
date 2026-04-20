using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for AssignmentSubmissions.
/// A StudentId + AssignmentId pair must be unique — one student, one submission per assignment.
/// Cascade delete from Assignment ensures submissions are cleaned up when an assignment is deleted.
/// </summary>
public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("AssignmentSubmissions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Unique Constraint: one submission per student per assignment ──
        builder.HasIndex(s => new { s.AssignmentId, s.StudentId })
            .IsUnique()
            .HasDatabaseName("IX_AssignmentSubmissions_AssignmentId_StudentId");

        // ── File Info ────────────────────────────────────────────
        builder.Property(s => s.OriginalFileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.StoredFileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.FileSizeBytes)
            .IsRequired();

        builder.Property(s => s.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        // ── Timestamps ──────────────────────────────────────────
        builder.Property(s => s.SubmittedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(s => s.UpdatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── FK: Assignment ───────────────────────────────────────
        // Cascade: when an Assignment is deleted, all its submissions are removed too
        builder.HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: Student ──────────────────────────────────────────
        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
