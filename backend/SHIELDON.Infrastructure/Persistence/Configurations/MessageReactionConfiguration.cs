using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SHIELDON.Domain.Entities;

namespace SHIELDON.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for MessageReaction.
/// Enforces unique constraint on MessageId + UserId and defines relationships.
/// </summary>
public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReactions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Emoji)
            .HasMaxLength(50)
            .IsRequired();

        // ── Unique Index to prevent duplicate reactions per user per message ──
        builder.HasIndex(r => new { r.MessageId, r.UserId })
            .IsUnique()
            .HasDatabaseName("IX_MessageReactions_MessageId_UserId");

        // ── Relationships ──────────────────────────────────────
        builder.HasOne(r => r.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
