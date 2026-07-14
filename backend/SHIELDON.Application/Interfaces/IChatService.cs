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
    Task<ChatMessageDto> SendGroupMessageAsync(Guid senderId, SendGroupMessageRequest request);

    /// <summary>
    /// Returns all users in the system that the current user can start a conversation with.
    /// </summary>
    Task<List<ChatUserDto>> GetUsersForChatAsync(Guid currentUserId);

    /// <summary>
    /// Returns the conversation ID between two users, or null if no conversation exists yet.
    /// </summary>
    Task<Guid?> GetConversationIdAsync(Guid userAId, Guid userBId);

    /// <summary>
    /// Creates a new group conversation. Restricted to Admin and Tutor roles.
    /// The creator is automatically added as a group admin participant.
    /// </summary>
    Task<ConversationSummaryDto> CreateGroupAsync(Guid creatorId, CreateGroupRequest request);

    /// <summary>
    /// Returns the list of participants in a group conversation.
    /// </summary>
    Task<List<GroupParticipantDto>> GetGroupParticipantsAsync(Guid conversationId);

    /// <summary>
    /// Renames a group conversation. Only the admin of the group can do this.
    /// </summary>
    Task<ConversationSummaryDto> RenameGroupAsync(Guid conversationId, Guid adminId, string newGroupName);

    /// <summary>
    /// Adds users to an existing group conversation. Only the admin can do this.
    /// </summary>
    Task AddGroupMembersAsync(Guid conversationId, Guid adminId, List<Guid> userIdsToAdd);

    /// <summary>
    /// Removes a user from a group conversation. Only the admin can do this.
    /// </summary>
    Task RemoveGroupMemberAsync(Guid conversationId, Guid adminId, Guid userIdToRemove);

    /// <summary>
    /// Permanently deletes a group conversation. Only the admin can do this.
    /// </summary>
    Task DeleteGroupAsync(Guid conversationId, Guid adminId);

    /// <summary>
    /// Adds, updates, or toggles/removes an emoji reaction on a message.
    /// Returns the updated list of reactions for the message.
    /// </summary>
    Task<(Guid ConversationId, List<MessageReactionDto> Reactions)> ReactToMessageAsync(Guid userId, Guid messageId, string emoji);

    /// <summary>
    /// Deletes a message. Restricted to the sender or the group's admin.
    /// Returns the conversation ID of the message.
    /// </summary>
    Task<Guid> DeleteMessageAsync(Guid userId, Guid messageId);

    /// <summary>
    /// Forwards a message to one or more target conversations.
    /// Returns the list of created messages.
    /// </summary>
    Task<List<ChatMessageDto>> ForwardMessageAsync(Guid userId, ForwardMessageRequest request);

    /// <summary>
    /// Returns the list of all participant user IDs in a conversation (both DMs and Groups).
    /// </summary>
    Task<List<Guid>> GetConversationParticipantUserIdsAsync(Guid conversationId);
}
