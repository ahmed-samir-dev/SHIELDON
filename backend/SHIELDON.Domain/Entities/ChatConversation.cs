namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a conversation between two users (direct message) or multiple users (group chat).
/// For direct messages, InitiatorId and ParticipantId identify the two parties.
/// For group chats (IsGroup == true), participants are tracked in the ConversationParticipants collection.
/// </summary>
public class ChatConversation
{
    public Guid Id { get; set; }

    // ── Direct Message Participants ───────────────────────────
    /// <summary>
    /// The ID of the user who initiated the conversation.
    /// For group chats, this is the Admin/Tutor who created the group.
    /// </summary>
    public Guid InitiatorId { get; set; }

    /// <summary>
    /// The ID of the user who received the initial message.
    /// Null for group conversations (participants tracked via ConversationParticipants).
    /// </summary>
    public Guid? ParticipantId { get; set; }

    // ── Group Chat ─────────────────────────────────────────────
    /// <summary>True when this conversation is a group chat. Created by Admin/Tutor only.</summary>
    public bool IsGroup { get; set; } = false;

    /// <summary>Display name of the group. Only relevant when IsGroup is true.</summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Relative URL path to the group's avatar image.
    /// Null for direct messages and groups without a custom icon.
    /// </summary>
    public string? GroupIconUrl { get; set; }

    // ── Timestamps ─────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last message sent. Used for sorting the inbox.</summary>
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ──────────────────────────────────
    public User Initiator { get; set; } = null!;
    public User? Participant { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = [];
    public ICollection<ConversationParticipant> Participants { get; set; } = [];
}
