using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ChatMessage.
/// </summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(m => m.Id);

        // ── Column Constraints ────────────────────────────────
        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(2000);

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

        builder.HasIndex(m => new { m.ConversationId, m.IsRead })
            .HasDatabaseName("IX_ChatMessages_ConversationId_IsRead");
    }
}
