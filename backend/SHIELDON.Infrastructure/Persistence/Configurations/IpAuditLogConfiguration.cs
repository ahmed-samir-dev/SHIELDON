using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the IpAuditLog entity.
/// Indexes are optimised for the two primary query patterns:
///   1. "All logs for a user" — (UserId, OccurredAt)
///   2. "All logs for an attempt" — (ExamAttemptId, OccurredAt)
///   3. "All logs matching an IP" — IpAddress (for cross-user VPN detection)
/// </summary>
public class IpAuditLogConfiguration : IEntityTypeConfiguration<IpAuditLog>
{
    public void Configure(EntityTypeBuilder<IpAuditLog> builder)
    {
        builder.ToTable("IpAuditLogs");
        builder.HasKey(x => x.Id);

        // ── Properties ──────────────────────────────────────────────────────
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45)       // max IPv6 with zone ID
            .IsRequired(false);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.IsVpnOrProxy).IsRequired();
        builder.Property(x => x.IsDuplicateSession).IsRequired();
        builder.Property(x => x.IsNetworkChangeDuringExam).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();

        // ── Relationships ────────────────────────────────────────────────────
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ExamAttempt)
            .WithMany()
            .HasForeignKey(x => x.ExamAttemptId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);   // avoid multiple cascade paths

        // ── Indexes ──────────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.UserId, x.OccurredAt })
            .HasDatabaseName("IX_IpAuditLogs_UserId_OccurredAt");

        builder.HasIndex(x => new { x.ExamAttemptId, x.OccurredAt })
            .HasDatabaseName("IX_IpAuditLogs_AttemptId_OccurredAt");

        builder.HasIndex(x => x.IpAddress)
            .HasDatabaseName("IX_IpAuditLogs_IpAddress");
    }
}
