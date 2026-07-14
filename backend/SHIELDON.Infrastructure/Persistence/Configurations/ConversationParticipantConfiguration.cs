using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ConversationParticipant.
/// Uses a composite primary key (ConversationId + UserId).
/// Used exclusively for group chat membership tracking.
/// </summary>
public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants");

        // ── Composite Primary Key ─────────────────────────────
        builder.HasKey(p => new { p.ConversationId, p.UserId });

        // ── Relationships ──────────────────────────────────────
        // Conversation → Participants is configured in ChatConversationConfiguration

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Performance Index ─────────────────────────────────
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_ConversationParticipants_UserId");
    }
}
