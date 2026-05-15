namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a single message sent within a ChatConversation.
/// </summary>
public class ChatMessage
{
    public Guid Id { get; set; }

    // ── Relationships ──────────────────────────────────────────
    /// <summary>The conversation this message belongs to.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>The user who sent this message.</summary>
    public Guid SenderId { get; set; }

    // ── Content ────────────────────────────────────────────────
    /// <summary>The text content of the message. Max 2000 characters.</summary>
    public string Content { get; set; } = string.Empty;

    // ── Read State ─────────────────────────────────────────────
    /// <summary>True once the recipient has read this message. Used to show unread counts.</summary>
    public bool IsRead { get; set; } = false;

    // ── Timestamps ─────────────────────────────────────────────
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ──────────────────────────────────
    public ChatConversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
