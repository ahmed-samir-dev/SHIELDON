using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for Announcements.
/// Important-priority announcements appear first and receive immediate notifications.
/// </summary>
public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Content ─────────────────────────────────────────────
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Content)
            .IsRequired()
            .HasColumnType("NVARCHAR(MAX)");

        // ── Priority ─────────────────────────────────────────────
        builder.Property(a => a.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // ── Attachments ──────────────────────────────────────────
        builder.Property(a => a.AttachmentPath)
            .HasMaxLength(500);

        builder.Property(a => a.AttachmentUrl)
            .HasMaxLength(2000);

        // ── Timestamps ──────────────────────────────────────────
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── FK: Course ──────────────────────────────────────────
        builder.HasOne(a => a.Course)
            .WithMany(c => c.Announcements)
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
