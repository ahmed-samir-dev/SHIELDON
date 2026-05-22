using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for Assignments.
/// An Assignment is a task published by a Tutor/Admin. Students submit their answer
/// files as AssignmentSubmission records linked to this entity.
/// </summary>
public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Content ─────────────────────────────────────────────
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Instructions)
            .HasColumnType("NVARCHAR(MAX)");

        // ── Reference File (all nullable - file is optional) ────
        builder.Property(a => a.ReferenceFileName)
            .HasMaxLength(500);

        builder.Property(a => a.ReferenceStoredFileName)
            .HasMaxLength(500);

        builder.Property(a => a.ReferenceFilePath)
            .HasMaxLength(500);

        builder.Property(a => a.ReferenceFileSizeBytes);

        builder.Property(a => a.ReferenceContentType)
            .HasMaxLength(200);

        // ── Deadline ─────────────────────────────────────────────
        builder.Property(a => a.DueDate)
            .HasColumnType("DATETIME2");

        // ── Weight ──────────────────────────────────────────────
        builder.Property(a => a.Weight)
            .HasPrecision(5, 2);

        // ── Timestamps ──────────────────────────────────────────
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── FK: Course ──────────────────────────────────────────
        // Cascade: deleting a course removes all its assignments (and via cascade, submissions)
        builder.HasOne(a => a.Course)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.CourseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: CreatedByUser ────────────────────────────────────
        builder.HasOne(a => a.CreatedByUser)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
