namespace SHIELDON.Application.Features.Chat.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

/// <summary>Request payload to send a new message in a conversation.</summary>
public class SendMessageRequest
{
    /// <summary>The ID of the user to send the message to. If no conversation exists, one is created.</summary>
    public Guid RecipientId { get; set; }

    /// <summary>The text content of the message. Max 2000 characters.</summary>
    public string Content { get; set; } = string.Empty;
}

// ── Responses ─────────────────────────────────────────────────────────────────

/// <summary>A single message in a conversation, returned to clients.</summary>
public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }

    /// <summary>True if this message was sent by the currently authenticated user.</summary>
    public bool IsOwnMessage { get; set; }
}

/// <summary>
/// A summary of a conversation shown in the inbox list.
/// Includes the other participant's info and the last message preview.
/// </summary>
public class ConversationSummaryDto
{
    public Guid ConversationId { get; set; }
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserAvatarUrl { get; set; }
    public string OtherUserRole { get; set; } = string.Empty;
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }

    /// <summary>Number of messages in this conversation the current user has not read.</summary>
    public int UnreadCount { get; set; }
}

/// <summary>A user available to start a chat with — used to populate the "New Chat" user list.</summary>
public class ChatUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}
