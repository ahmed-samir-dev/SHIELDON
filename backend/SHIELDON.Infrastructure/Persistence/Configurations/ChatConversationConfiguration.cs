using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ChatConversation.
/// Supports both direct messages and group chats (IsGroup flag).
/// </summary>
public class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("ChatConversations");
        builder.HasKey(c => c.Id);

        // ── Group Chat Columns ─────────────────────────────────
        builder.Property(c => c.GroupName)
            .HasMaxLength(100);

        builder.Property(c => c.GroupIconUrl)
            .HasMaxLength(500);

        // ── Relationships ──────────────────────────────────────
        builder.HasOne(c => c.Initiator)
            .WithMany()
            .HasForeignKey(c => c.InitiatorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ParticipantId is now nullable (null for group chats)
        builder.HasOne(c => c.Participant)
            .WithMany()
            .HasForeignKey(c => c.ParticipantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Participants)
            .WithOne(p => p.Conversation)
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Unique Constraint ──────────────────────────────────
        // Only enforced for DM pairs (IsGroup == false).
        // Application logic always stores with the lower GUID as InitiatorId.
        builder.HasIndex(c => new { c.InitiatorId, c.ParticipantId })
            .HasDatabaseName("IX_ChatConversations_UserPair");

        // ── Performance Index ─────────────────────────────────
        builder.HasIndex(c => c.LastMessageAt)
            .HasDatabaseName("IX_ChatConversations_LastMessageAt");

        builder.HasIndex(c => c.IsGroup)
            .HasDatabaseName("IX_ChatConversations_IsGroup");
    }
}
