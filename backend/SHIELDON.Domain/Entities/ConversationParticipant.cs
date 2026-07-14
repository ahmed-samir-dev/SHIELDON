namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a participant in a group conversation.
/// Maps a User to a ChatConversation with an optional admin privilege flag.
/// Used exclusively for group conversations (ChatConversation.IsGroup == true).
/// </summary>
public class ConversationParticipant
{
    // ── Composite Key Fields ────────────────────────────────────
    /// <summary>The group conversation this participant belongs to.</summary>
    public Guid ConversationId { get; set; }

    /// <summary>The user who is a member of the group.</summary>
    public Guid UserId { get; set; }

    // ── Privileges ─────────────────────────────────────────────
    /// <summary>
    /// When true, this participant is a group admin (typically the Admin or Tutor who created it).
    /// Group admins can add/remove members and update group info.
    /// </summary>
    public bool IsAdmin { get; set; } = false;

    // ── Timestamps ─────────────────────────────────────────────
    /// <summary>UTC timestamp when the user was added to the group.</summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation Properties ──────────────────────────────────
    public ChatConversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}
