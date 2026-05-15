namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a direct-message conversation between exactly two users.
/// A conversation is created on-demand the first time either user messages the other.
/// </summary>
public class ChatConversation
{
    public Guid Id { get; set; }

    // ── Participants ───────────────────────────────────────────
    /// <summary>The ID of the user who initiated the conversation.</summary>
    public Guid InitiatorId { get; set; }

    /// <summary>The ID of the user who received the initial message.</summary>
    public Guid ParticipantId { get; set; }

    // ── Timestamps ─────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last message sent in this conversation. Used for sorting the inbox.</summary>
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ──────────────────────────────────
    public User Initiator { get; set; } = null!;
    public User Participant { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = [];
}
