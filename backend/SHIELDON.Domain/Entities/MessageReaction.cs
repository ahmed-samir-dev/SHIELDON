using System;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a user's emoji reaction to a specific chat message.
/// A user can have at most one active reaction on any given message.
/// </summary>
public class MessageReaction
{
    public Guid Id { get; set; }

    /// <summary>The ID of the message this reaction belongs to.</summary>
    public Guid MessageId { get; set; }

    /// <summary>The ID of the user who made the reaction.</summary>
    public Guid UserId { get; set; }

    /// <summary>The emoji string, e.g. "👍", "❤️", "😂", "😮", "😢", "😡".</summary>
    public string Emoji { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the user reacted to the message.</summary>
    public DateTime ReactedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ChatMessage Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
