using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core table configuration for Notifications.
/// Includes index on RecipientUserId + IsRead for efficient unread-count queries.
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        // ── Content ─────────────────────────────────────────────
        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // ── Read Status ──────────────────────────────────────────
        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.ReadAt)
            .HasColumnType("DATETIME2");

        // ── Context Links ────────────────────────────────────────
        builder.Property(n => n.RelatedCourseId);
        builder.Property(n => n.RelatedExamId);

        // ── Timestamps ──────────────────────────────────────────
        builder.Property(n => n.CreatedAt)
            .IsRequired()
            .HasColumnType("DATETIME2");

        // ── Index: fast unread count + user notification queries ─
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead");

        // ── FK: RecipientUser ────────────────────────────────────
        builder.HasOne(n => n.RecipientUser)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
