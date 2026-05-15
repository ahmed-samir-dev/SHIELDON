using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Chat.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Handles all chat persistence: conversations, messages, and read-state management.
/// Real-time delivery is performed separately via ChatHub (SignalR).
/// </summary>
public class ChatService : IChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<ConversationSummaryDto>> GetInboxAsync(Guid currentUserId)
    {
        var conversations = await _db.ChatConversations
            .Where(c => c.InitiatorId == currentUserId || c.ParticipantId == currentUserId)
            .Include(c => c.Initiator)
            .Include(c => c.Participant)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        return conversations.Select(c =>
        {
            var otherUser = c.InitiatorId == currentUserId ? c.Participant : c.Initiator;
            var lastMsg = c.Messages.FirstOrDefault();

            var unreadCount = _db.ChatMessages
                .Count(m => m.ConversationId == c.Id
                         && m.SenderId != currentUserId
                         && !m.IsRead);

            return new ConversationSummaryDto
            {
                ConversationId = c.Id,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.FullName,
                OtherUserAvatarUrl = otherUser.ProfilePictureUrl,
                OtherUserRole = otherUser.Role.ToString(),
                LastMessagePreview = lastMsg?.Content.Length > 60
                    ? lastMsg.Content[..60] + "..."
                    : (lastMsg?.Content ?? ""),
                LastMessageAt = c.LastMessageAt,
                UnreadCount = unreadCount
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ChatMessageDto>> GetMessagesAsync(Guid conversationId, Guid currentUserId)
    {
        // Mark all unread messages from the other user as read
        await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && m.SenderId != currentUserId
                     && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

        var messages = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Sender)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return messages.Select(m => new ChatMessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderName = m.Sender.FullName,
            SenderAvatarUrl = m.Sender.ProfilePictureUrl,
            Content = m.Content,
            IsRead = m.IsRead,
            SentAt = m.SentAt,
            IsOwnMessage = m.SenderId == currentUserId
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<ChatMessageDto> SendMessageAsync(Guid senderId, SendMessageRequest request)
    {
        // Canonicalize the user-pair: always store lower GUID as Initiator.
        // This guarantees the unique index IX_ChatConversations_UserPair works correctly.
        var (initiatorId, participantId) = senderId < request.RecipientId
            ? (senderId, request.RecipientId)
            : (request.RecipientId, senderId);

        // Find existing conversation or create a new one
        var conversation = await _db.ChatConversations
            .FirstOrDefaultAsync(c => c.InitiatorId == initiatorId
                                   && c.ParticipantId == participantId);

        if (conversation is null)
        {
            conversation = new ChatConversation
            {
                Id = Guid.NewGuid(),
                InitiatorId = initiatorId,
                ParticipantId = participantId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _db.ChatConversations.Add(conversation);
        }
        else
        {
            conversation.LastMessageAt = DateTime.UtcNow;
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = request.Content.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        // Load the sender for the response
        var sender = await _db.Users.FindAsync(senderId);

        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = conversation.Id,
            SenderId = senderId,
            SenderName = sender?.FullName ?? "",
            SenderAvatarUrl = sender?.ProfilePictureUrl,
            Content = message.Content,
            IsRead = false,
            SentAt = message.SentAt,
            IsOwnMessage = true
        };
    }

    /// <inheritdoc />
    public async Task<List<ChatUserDto>> GetUsersForChatAsync(Guid currentUserId)
    {
        return await _db.Users
            .Where(u => u.Id != currentUserId)
            .OrderBy(u => u.FirstName)
            .Select(u => new ChatUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                AvatarUrl = u.ProfilePictureUrl,
                Role = u.Role.ToString()
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Guid?> GetConversationIdAsync(Guid userAId, Guid userBId)
    {
        var (initiatorId, participantId) = userAId < userBId
            ? (userAId, userBId)
            : (userBId, userAId);

        var conversation = await _db.ChatConversations
            .FirstOrDefaultAsync(c => c.InitiatorId == initiatorId
                                   && c.ParticipantId == participantId);

        return conversation?.Id;
    }
}
