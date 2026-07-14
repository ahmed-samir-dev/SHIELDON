using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ChatMessage.
/// Adds delivery status, attachment support, and updates the read-state index.
/// </summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(m => m.Id);

        // ── Column Constraints ────────────────────────────────
        builder.Property(m => m.Content)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);


        // Status stored as int; default Sent (0)
        builder.Property(m => m.Status)
            .HasConversion<int>()
            .HasDefaultValue(MessageStatus.Sent);

        // AttachmentType stored as int; default None (0)
        builder.Property(m => m.AttachmentType)
            .HasConversion<int>()
            .HasDefaultValue(AttachmentType.None);

        builder.Property(m => m.AttachmentUrl)
            .HasMaxLength(500);

        builder.HasOne(m => m.RepliedToMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(m => m.RepliedToMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Relationships ──────────────────────────────────────
        // Conversation → Messages is configured in ChatConversationConfiguration
        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Performance Indexes ───────────────────────────────
        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("IX_ChatMessages_ConversationId");

        builder.HasIndex(m => m.SentAt)
            .HasDatabaseName("IX_ChatMessages_SentAt");

        // Updated index: filter by Status instead of the old IsRead bool
        builder.HasIndex(m => new { m.ConversationId, m.Status })
            .HasDatabaseName("IX_ChatMessages_ConversationId_Status");
    }
}
