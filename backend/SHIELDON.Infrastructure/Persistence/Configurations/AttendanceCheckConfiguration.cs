using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

public class AttendanceCheckConfiguration : IEntityTypeConfiguration<AttendanceCheck>
{
    public void Configure(EntityTypeBuilder<AttendanceCheck> builder)
    {
        builder.ToTable("AttendanceChecks");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.CurrentSecret).HasMaxLength(64).IsRequired();

        // ── Relationships ──────────────────────────────────────
        builder.HasOne(a => a.Course)
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Tutor)
            .WithMany()
            .HasForeignKey(a => a.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Records)
            .WithOne(r => r.AttendanceCheck)
            .HasForeignKey(r => r.AttendanceCheckId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Performance Index ──────────────────────────────────
        // Quickly find all active checks (used by the background rotation service)
        builder.HasIndex(a => new { a.CourseId, a.IsActive })
            .HasDatabaseName("IX_AttendanceChecks_Course_Active");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_AttendanceChecks_CreatedAt");
    }
}
