using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for the Courses entity.
/// Enforces: unique CourseCode, required Title, FK constraints with restricted delete.
/// </summary>
public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Core Info ───────────────────────────────────────────
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CourseCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.CourseCode)
            .IsUnique()
            .HasDatabaseName("IX_Courses_CourseCode");

        builder.Property(c => c.CourseFee)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0.00m)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        // ── Status and Ownership ────────────────────────────────
        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // ── Timestamps ──────────────────────────────────────────
        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── FK: AssignedTutor (nullable — course may be unassigned) ──
        builder.HasOne(c => c.AssignedTutor)
            .WithMany()
            .HasForeignKey(c => c.AssignedTutorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ── FK: CreatedByAdmin ───────────────────────────────────
        builder.HasOne(c => c.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(c => c.CreatedByAdminId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
