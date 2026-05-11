using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for CourseMaterials.
/// Supports both uploaded files and external links via the MaterialType discriminator.
/// </summary>
public class CourseMaterialConfiguration : IEntityTypeConfiguration<CourseMaterial>
{
    public void Configure(EntityTypeBuilder<CourseMaterial> builder)
    {
        builder.ToTable("CourseMaterials");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Content ─────────────────────────────────────────────
        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.Property(m => m.MaterialType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        // ── File Info ────────────────────────────────────────────
        builder.Property(m => m.FilePath)
            .HasMaxLength(500);

        builder.Property(m => m.OriginalFileName)
            .HasMaxLength(300);

        builder.Property(m => m.ContentType)
            .HasMaxLength(100);

        builder.Property(m => m.FileSizeBytes);

        // ── Link Info ────────────────────────────────────────────
        builder.Property(m => m.ExternalUrl)
            .HasMaxLength(2000);

        // ── Timestamps ──────────────────────────────────────────
        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(m => m.UpdatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── FK: Course ──────────────────────────────────────────
        builder.HasOne(m => m.Course)
            .WithMany(c => c.Materials)
            .HasForeignKey(m => m.CourseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ── FK: UploadedByUser ───────────────────────────────────
        builder.HasOne(m => m.UploadedByUser)
            .WithMany()
            .HasForeignKey(m => m.UploadedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
