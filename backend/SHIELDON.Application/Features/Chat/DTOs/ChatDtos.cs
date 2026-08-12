using SHIELDON.Domain.Enums;

namespace SHIELDON.Application.Features.Chat.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

/// <summary>Request payload to send a new text message in a DM conversation.</summary>
public class SendMessageRequest
{
    /// <summary>The ID of the user to send the message to. If no DM conversation exists, one is created.</summary>
    public Guid RecipientId { get; set; }

    /// <summary>The text content of the message. Max 2000 characters. May be empty for attachment-only messages.</summary>
    public string Content { get; set; } = string.Empty;

    public string? AttachmentUrl { get; set; }
    public AttachmentType AttachmentType { get; set; } = AttachmentType.None;
    public Guid? RepliedToMessageId { get; set; }
}

/// <summary>Request payload to send a message into a group conversation.</summary>
public class SendGroupMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public AttachmentType AttachmentType { get; set; } = AttachmentType.None;
    public Guid? RepliedToMessageId { get; set; }
}

/// <summary>Request payload for Admin/Tutor to create a new group chat.</summary>
public class CreateGroupRequest
{
    /// <summary>Display name for the group. Required.</summary>
    public string GroupName { get; set; } = string.Empty;

    public List<Guid> MemberIds { get; set; } = [];
}

/// <summary>Request payload for renaming an existing group.</summary>
public class RenameGroupRequest
{
    public string NewGroupName { get; set; } = string.Empty;
}

/// <summary>Request payload for adding new members to an existing group.</summary>
public class AddGroupMembersRequest
{
    public List<Guid> MemberIds { get; set; } = [];
}

/// <summary>WebRTC signaling payload sent via SignalR for 1-on-1 calls.</summary>
public class WebRtcSignalDto
{
    /// <summary>The target user's connection ID or user ID to route the signal to.</summary>
    public Guid TargetUserId { get; set; }

    /// <summary>Serialised SDP offer, SDP answer, or ICE candidate JSON string.</summary>
    public string Signal { get; set; } = string.Empty;
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

    /// <summary>WhatsApp-style delivery status: Sent → Delivered → Read.</summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    /// <summary>Attachment type for rendering logic (None, Audio, Image, Document).</summary>
    public AttachmentType AttachmentType { get; set; } = AttachmentType.None;

    /// <summary>Relative URL to the uploaded attachment file. Null for plain text messages.</summary>
    public string? AttachmentUrl { get; set; }

    public DateTime SentAt { get; set; }

    /// <summary>True if this message was sent by the currently authenticated user.</summary>
    public bool IsOwnMessage { get; set; }

    public bool IsDeleted { get; set; }
    public bool IsForwarded { get; set; }

    public Guid? RepliedToMessageId { get; set; }
    public string? RepliedToMessageContent { get; set; }
    public string? RepliedToMessageSenderName { get; set; }
    public AttachmentType? RepliedToMessageAttachmentType { get; set; }

    public List<MessageReactionDto> Reactions { get; set; } = [];
}

/// <summary>
/// A summary of a conversation shown in the inbox list.
/// Includes the other participant's info and the last message preview.
/// </summary>
public class ConversationSummaryDto
{
    public Guid ConversationId { get; set; }

    /// <summary>Null for group conversations.</summary>
    public Guid? OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserAvatarUrl { get; set; }
    public string OtherUserRole { get; set; } = string.Empty;

    /// <summary>True when this is a group conversation.</summary>
    public bool IsGroup { get; set; }
    public string? GroupName { get; set; }
    public string? GroupIconUrl { get; set; }

    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }

    /// <summary>Number of unread messages (Status != Read) from other participants.</summary>
    public int UnreadCount { get; set; }

    /// <summary>Last seen timestamp of the other user in 1-on-1 DM. Null for groups.</summary>
    public DateTime? OtherUserLastSeenAt { get; set; }
}

/// <summary>A user available to start a chat with - used to populate the "New Chat" user list.</summary>
public class ChatUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;

    /// <summary>True when the user has an active PresenceLog heartbeat.</summary>
    public bool IsOnline { get; set; }

    /// <summary>Last seen timestamp of the user.</summary>
    public DateTime? LastSeenAt { get; set; }
}

/// <summary>Response returned after a successful file upload for a chat attachment.</summary>
public class AttachmentUploadResponse
{
    /// <summary>Relative URL path to access the uploaded file.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Categorised type for the frontend rendering engine.</summary>
    public AttachmentType AttachmentType { get; set; }
}

/// <summary>Represents a member in a group conversation.</summary>
public class GroupParticipantDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsAdmin { get; set; }
}

/// <summary>Represents a user reaction to a message.</summary>
public class MessageReactionDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
}

/// <summary>Request payload to react to a message.</summary>
public class ReactToMessageRequest
{
    public string Emoji { get; set; } = string.Empty;
}

/// <summary>Request payload to forward a message.</summary>
public class ForwardMessageRequest
{
    public Guid MessageId { get; set; }
    public List<Guid> TargetConversationIds { get; set; } = [];
}
