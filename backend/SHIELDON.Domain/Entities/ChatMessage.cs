using SHIELDON.Domain.Enums;

namespace SHIELDON.Domain.Entities;

/// <summary>
/// Represents a single message sent within a ChatConversation.
/// Supports plain text, voice notes, images, and document attachments.
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
    /// <summary>The text content of the message. Max 2000 characters. May be empty for attachment-only messages.</summary>
    public string Content { get; set; } = string.Empty;

    // ── Delivery Status ────────────────────────────────────────
    /// <summary>
    /// WhatsApp-style delivery progression: Sent → Delivered → Read.
    /// Updated via SignalR hub events MarkAsDelivered and MarkAsRead.
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    // ── Attachment ─────────────────────────────────────────────
    /// <summary>
    /// Relative URL path to the uploaded file (e.g. /uploads/chat/audio/guid.webm).
    /// Null for plain text messages.
    /// </summary>
    public string? AttachmentUrl { get; set; }

    /// <summary>
    /// Categorises the attachment for frontend rendering logic.
    /// None = plain text, Audio = voice note, Image = photo, Document = PDF/DOCX.
    /// </summary>
    public AttachmentType AttachmentType { get; set; } = AttachmentType.None;

    // ── Timestamps ─────────────────────────────────────────────
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // ── Enhancements ───────────────────────────────────────────
    /// <summary>True if the message has been deleted for everyone by the sender or group admin.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>True if the message was forwarded from another conversation.</summary>
    public bool IsForwarded { get; set; } = false;

    /// <summary>The ID of the message this message is replying to, if any.</summary>
    public Guid? RepliedToMessageId { get; set; }

    // ── Navigation Properties ──────────────────────────────────
    public ChatConversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ChatMessage? RepliedToMessage { get; set; }
    public ICollection<ChatMessage> Replies { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
}
