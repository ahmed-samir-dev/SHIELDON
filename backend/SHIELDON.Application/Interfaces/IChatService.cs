using SHIELDON.Application.Features.Chat.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Defines the contract for the chat persistence service.
/// Handles conversation/message CRUD. Real-time delivery is handled separately by ChatHub.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Returns all conversations for the current user, sorted by most recent message.
    /// Each entry includes unread count and last message preview.
    /// </summary>
    Task<List<ConversationSummaryDto>> GetInboxAsync(Guid currentUserId);

    /// <summary>
    /// Returns the full message history for a conversation.
    /// Automatically marks all unread messages from the other user as read.
    /// </summary>
    Task<List<ChatMessageDto>> GetMessagesAsync(Guid conversationId, Guid currentUserId);

    /// <summary>
    /// Persists a new message. Creates the conversation if it does not exist.
    /// Returns the saved message DTO to be broadcast via SignalR.
    /// </summary>
    Task<ChatMessageDto> SendMessageAsync(Guid senderId, SendMessageRequest request);

    /// <summary>
    /// Returns all users in the system that the current user can start a conversation with.
    /// </summary>
    Task<List<ChatUserDto>> GetUsersForChatAsync(Guid currentUserId);

    /// <summary>
    /// Returns the conversation ID between two users, or null if no conversation exists yet.
    /// </summary>
    Task<Guid?> GetConversationIdAsync(Guid userAId, Guid userBId);
}
