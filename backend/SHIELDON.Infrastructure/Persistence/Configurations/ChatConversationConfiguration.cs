using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ChatConversation.
/// Enforces the uniqueness constraint: only one conversation can exist per pair of users.
/// </summary>
public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("ChatConversations");
        builder.HasKey(c => c.Id);

        // ── Relationships ──────────────────────────────────────
        builder.HasOne(c => c.Initiator)
            .WithMany()
            .HasForeignKey(c => c.InitiatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Participant)
            .WithMany()
            .HasForeignKey(c => c.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Unique Constraint ──────────────────────────────────
        // Prevents two separate rows for the same pair of users (A→B and B→A).
        // Application logic always stores them with the lower GUID as InitiatorId.
        builder.HasIndex(c => new { c.InitiatorId, c.ParticipantId })
            .IsUnique()
            .HasDatabaseName("IX_ChatConversations_UserPair");

        // ── Performance Index ─────────────────────────────────
        builder.HasIndex(c => c.LastMessageAt)
            .HasDatabaseName("IX_ChatConversations_LastMessageAt");
    }
}
