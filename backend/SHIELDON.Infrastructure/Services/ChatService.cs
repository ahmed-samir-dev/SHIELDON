using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Chat.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Handles all chat persistence: DM conversations, group conversations, messages, and read-state management.
/// Real-time delivery (Delivered/Read receipts and WebRTC signaling) is handled separately via ChatHub (SignalR).
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
        // Fetch both DM and group conversations the user participates in
        var dmConversations = await _db.ChatConversations
            .AsNoTracking()
            .Where(c => !c.IsGroup && (c.InitiatorId == currentUserId || c.ParticipantId == currentUserId))
            .Include(c => c.Initiator)
            .Include(c => c.Participant)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        var groupConversations = await _db.ChatConversations
            .AsNoTracking()
            .Where(c => c.IsGroup && c.Participants.Any(p => p.UserId == currentUserId))
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        // Batch fetch all unread counts in one query — avoids N+1
        var allConversationIds = dmConversations.Select(c => c.Id)
            .Concat(groupConversations.Select(c => c.Id))
            .ToList();

        var unreadCounts = await _db.ChatMessages
            .Where(m => allConversationIds.Contains(m.ConversationId)
                     && m.SenderId != currentUserId
                     && m.Status != MessageStatus.Read)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

        var dmResults = dmConversations.Select(c =>
        {
            var otherUser = c.InitiatorId == currentUserId ? c.Participant : c.Initiator;
            var lastMsg = c.Messages.FirstOrDefault();

            return new ConversationSummaryDto
            {
                ConversationId = c.Id,
                OtherUserId = otherUser?.Id,
                OtherUserName = otherUser?.FullName ?? "",
                OtherUserAvatarUrl = otherUser?.ProfilePictureUrl,
                OtherUserRole = otherUser?.Role.ToString() ?? "",
                IsGroup = false,
                LastMessagePreview = BuildPreview(lastMsg),
                LastMessageAt = c.LastMessageAt,
                UnreadCount = unreadCounts.GetValueOrDefault(c.Id, 0),
                OtherUserLastSeenAt = otherUser?.LastSeenAt
            };
        });

        var groupResults = groupConversations.Select(c =>
        {
            var lastMsg = c.Messages.FirstOrDefault();

            return new ConversationSummaryDto
            {
                ConversationId = c.Id,
                IsGroup = true,
                GroupName = c.GroupName,
                GroupIconUrl = c.GroupIconUrl,
                LastMessagePreview = BuildPreview(lastMsg),
                LastMessageAt = c.LastMessageAt,
                UnreadCount = unreadCounts.GetValueOrDefault(c.Id, 0)
            };
        });

        return dmResults.Concat(groupResults)
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<ChatMessageDto>> GetMessagesAsync(Guid conversationId, Guid currentUserId)
    {
        var messages = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.Sender)
            .Include(m => m.RepliedToMessage)
                .ThenInclude(rm => rm!.Sender)
            .Include(m => m.Reactions)
                .ThenInclude(r => r!.User)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return messages.Select(m => MapToDto(m, currentUserId)).ToList();
    }

    /// <inheritdoc />
    public async Task<ChatMessageDto> SendMessageAsync(Guid senderId, SendMessageRequest request)
    {
        // Canonicalize the user-pair: always store lower GUID as Initiator.
        var (initiatorId, participantId) = senderId < request.RecipientId
            ? (senderId, request.RecipientId)
            : (request.RecipientId, senderId);

        // Find existing DM conversation or create a new one
        var conversation = await _db.ChatConversations
            .FirstOrDefaultAsync(c => !c.IsGroup
                                   && c.InitiatorId == initiatorId
                                   && c.ParticipantId == participantId);

        if (conversation is null)
        {
            conversation = new ChatConversation
            {
                Id = Guid.NewGuid(),
                InitiatorId = initiatorId,
                ParticipantId = participantId,
                IsGroup = false,
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
            Content = SHIELDON.Infrastructure.Common.SanitizationHelper.StripHtml(request.Content),
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType,
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow,
            RepliedToMessageId = request.RepliedToMessageId
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        var savedMessage = await _db.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.RepliedToMessage)
                .ThenInclude(rm => rm!.Sender)
            .Include(m => m.Reactions)
                .ThenInclude(r => r!.User)
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        if (savedMessage == null) throw new InvalidOperationException("Failed to save message.");

        return MapToDto(savedMessage, senderId);
    }

    /// <inheritdoc />
    public async Task<List<ChatUserDto>> GetUsersForChatAsync(Guid currentUserId)
    {
        // IsOnline will be set dynamically by ChatHub via in-memory presence tracking (Step 6).
        return await _db.Users
            .Where(u => u.Id != currentUserId)
            .OrderBy(u => u.FirstName)
            .Select(u => new ChatUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                AvatarUrl = u.ProfilePictureUrl,
                Role = u.Role.ToString(),
                IsOnline = false,
                LastSeenAt = u.LastSeenAt
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
            .FirstOrDefaultAsync(c => !c.IsGroup
                                   && c.InitiatorId == initiatorId
                                   && c.ParticipantId == participantId);

        return conversation?.Id;
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private static ChatMessageDto MapToDto(ChatMessage m, Guid currentUserId) => new()
    {
        Id = m.Id,
        ConversationId = m.ConversationId,
        SenderId = m.SenderId,
        SenderName = m.Sender?.FullName ?? "Unknown",
        SenderAvatarUrl = m.Sender?.ProfilePictureUrl,
        Content = m.IsDeleted ? "This message was deleted" : m.Content,
        Status = m.Status,
        AttachmentType = m.IsDeleted ? AttachmentType.None : m.AttachmentType,
        AttachmentUrl = m.IsDeleted ? null : m.AttachmentUrl,
        SentAt = m.SentAt,
        IsOwnMessage = m.SenderId == currentUserId,
        IsDeleted = m.IsDeleted,
        IsForwarded = m.IsForwarded,
        RepliedToMessageId = m.RepliedToMessageId,
        RepliedToMessageContent = m.RepliedToMessage != null
            ? (m.RepliedToMessage.IsDeleted ? "This message was deleted" : m.RepliedToMessage.Content)
            : null,
        RepliedToMessageSenderName = m.RepliedToMessage?.Sender?.FullName,
        RepliedToMessageAttachmentType = m.RepliedToMessage?.AttachmentType,
        Reactions = m.Reactions?.Select(r => new MessageReactionDto
        {
            UserId = r.UserId,
            UserName = r.User?.FullName ?? "Unknown",
            Emoji = r.Emoji
        }).ToList() ?? new List<MessageReactionDto>()
    };

    private static string BuildPreview(ChatMessage? msg)
    {
        if (msg is null) return "";
        if (msg.IsDeleted) return "🗑️ This message was deleted";
        if (msg.AttachmentType == AttachmentType.Audio) return "🎤 Voice note";
        if (msg.AttachmentType == AttachmentType.Image) return "📷 Photo";
        if (msg.AttachmentType == AttachmentType.Document) return "📄 Document";
        return msg.Content.Length > 60 ? msg.Content[..60] + "..." : msg.Content;
    }

    public async Task<ChatMessageDto> SendGroupMessageAsync(Guid senderId, SendGroupMessageRequest request)
    {
        var conversation = await _db.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId && c.IsGroup);

        if (conversation is null)
            throw new KeyNotFoundException("Group conversation not found.");

        var isParticipant = await _db.ConversationParticipants
            .AnyAsync(p => p.ConversationId == request.ConversationId && p.UserId == senderId);

        if (!isParticipant)
            throw new UnauthorizedAccessException("You are not a participant in this group.");

        conversation.LastMessageAt = DateTime.UtcNow;

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = request.Content?.Trim() ?? string.Empty,
            AttachmentUrl = request.AttachmentUrl,
            AttachmentType = request.AttachmentType,
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow,
            RepliedToMessageId = request.RepliedToMessageId
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        var savedMessage = await _db.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.RepliedToMessage)
                .ThenInclude(rm => rm!.Sender)
            .Include(m => m.Reactions)
                .ThenInclude(r => r!.User)
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        if (savedMessage == null) throw new InvalidOperationException("Failed to save message.");

        return MapToDto(savedMessage, senderId);
    }

    // ── Group Methods ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ConversationSummaryDto> CreateGroupAsync(Guid creatorId, CreateGroupRequest request)
    {
        var group = new ChatConversation
        {
            Id = Guid.NewGuid(),
            InitiatorId = creatorId,
            IsGroup = true,
            GroupName = request.GroupName.Trim(),
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };

        _db.ChatConversations.Add(group);

        // Add creator as group admin
        _db.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationId = group.Id,
            UserId = creatorId,
            IsAdmin = true,
            JoinedAt = DateTime.UtcNow
        });

        // Add all specified members
        foreach (var memberId in request.MemberIds.Distinct().Where(id => id != creatorId))
        {
            _db.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = group.Id,
                UserId = memberId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return new ConversationSummaryDto
        {
            ConversationId = group.Id,
            IsGroup = true,
            GroupName = group.GroupName,
            GroupIconUrl = group.GroupIconUrl,
            LastMessagePreview = "",
            LastMessageAt = group.LastMessageAt,
            UnreadCount = 0
        };
    }

    /// <inheritdoc />
    public async Task<List<GroupParticipantDto>> GetGroupParticipantsAsync(Guid conversationId)
    {
        return await _db.ConversationParticipants
            .Where(p => p.ConversationId == conversationId)
            .Include(p => p.User)
            .Select(p => new GroupParticipantDto
            {
                UserId = p.UserId,
                FullName = p.User.FullName,
                AvatarUrl = p.User.ProfilePictureUrl,
                IsAdmin = p.IsAdmin
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ConversationSummaryDto> RenameGroupAsync(Guid conversationId, Guid adminId, string newGroupName)
    {
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.IsGroup);
        if (conversation == null) throw new InvalidOperationException("Group not found.");

        var participant = await _db.ConversationParticipants.FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == adminId);
        if (participant == null || !participant.IsAdmin) throw new InvalidOperationException("Only group admins can rename the group.");

        conversation.GroupName = newGroupName;
        await _db.SaveChangesAsync();

        return new ConversationSummaryDto
        {
            ConversationId = conversation.Id,
            IsGroup = true,
            GroupName = conversation.GroupName,
            GroupIconUrl = conversation.GroupIconUrl,
            LastMessagePreview = "",
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = 0
        };
    }

    /// <inheritdoc />
    public async Task AddGroupMembersAsync(Guid conversationId, Guid adminId, List<Guid> userIdsToAdd)
    {
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.IsGroup);
        if (conversation == null) throw new InvalidOperationException("Group not found.");

        var participant = await _db.ConversationParticipants.FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == adminId);
        if (participant == null || !participant.IsAdmin) throw new InvalidOperationException("Only group admins can add members.");

        var existingMembers = await _db.ConversationParticipants
            .Where(p => p.ConversationId == conversationId)
            .Select(p => p.UserId)
            .ToListAsync();

        foreach (var userId in userIdsToAdd.Distinct())
        {
            if (!existingMembers.Contains(userId))
            {
                _db.ConversationParticipants.Add(new ConversationParticipant
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsAdmin = false,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveGroupMemberAsync(Guid conversationId, Guid adminId, Guid userIdToRemove)
    {
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.IsGroup);
        if (conversation == null) throw new KeyNotFoundException("Group not found");

        var isAdmin = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == adminId && p.IsAdmin);
        if (!isAdmin) throw new UnauthorizedAccessException("Only group admins can remove members");

        var memberToRemove = await _db.ConversationParticipants.FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userIdToRemove);
        if (memberToRemove != null)
        {
            _db.ConversationParticipants.Remove(memberToRemove);
            await _db.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task DeleteGroupAsync(Guid conversationId, Guid adminId)
    {
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.IsGroup);
        if (conversation == null) throw new KeyNotFoundException("Group not found");

        var isAdmin = await _db.ConversationParticipants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == adminId && p.IsAdmin);
        if (!isAdmin) throw new UnauthorizedAccessException("Only group admins can delete the group");

        _db.ChatConversations.Remove(conversation);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(Guid ConversationId, List<MessageReactionDto> Reactions)> ReactToMessageAsync(Guid userId, Guid messageId, string emoji)
    {
        var message = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null) throw new KeyNotFoundException("Message not found.");

        var existingReaction = await _db.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (existingReaction != null)
        {
            if (existingReaction.Emoji == emoji)
            {
                _db.MessageReactions.Remove(existingReaction);
            }
            else
            {
                existingReaction.Emoji = emoji;
                existingReaction.ReactedAt = DateTime.UtcNow;
            }
        }
        else
        {
            var newReaction = new MessageReaction
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                ReactedAt = DateTime.UtcNow
            };
            _db.MessageReactions.Add(newReaction);
        }

        await _db.SaveChangesAsync();

        var reactionsList = await _db.MessageReactions
            .Where(r => r.MessageId == messageId)
            .Include(r => r.User)
            .Select(r => new MessageReactionDto
            {
                UserId = r.UserId,
                UserName = r.User.FullName,
                Emoji = r.Emoji
            })
            .ToListAsync();

        return (message.ConversationId, reactionsList);
    }

    /// <inheritdoc />
    public async Task<Guid> DeleteMessageAsync(Guid userId, Guid messageId)
    {
        var message = await _db.ChatMessages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null) throw new KeyNotFoundException("Message not found.");

        bool isSender = message.SenderId == userId;
        bool isGroupAdmin = false;

        if (message.Conversation.IsGroup)
        {
            isGroupAdmin = await _db.ConversationParticipants
                .AnyAsync(p => p.ConversationId == message.ConversationId && p.UserId == userId && p.IsAdmin);
        }

        if (!isSender && !isGroupAdmin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this message.");
        }

        message.Content = "";
        message.AttachmentUrl = null;
        message.AttachmentType = AttachmentType.None;
        message.IsDeleted = true;

        var reactions = await _db.MessageReactions.Where(r => r.MessageId == messageId).ToListAsync();
        _db.MessageReactions.RemoveRange(reactions);

        await _db.SaveChangesAsync();

        return message.ConversationId;
    }

    /// <inheritdoc />
    public async Task<List<ChatMessageDto>> ForwardMessageAsync(Guid userId, ForwardMessageRequest request)
    {
        var originalMessage = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == request.MessageId);
        if (originalMessage == null) throw new KeyNotFoundException("Original message not found.");
        if (originalMessage.IsDeleted) throw new InvalidOperationException("Cannot forward a deleted message.");

        var results = new List<ChatMessageDto>();

        foreach (var targetConvId in request.TargetConversationIds.Distinct())
        {
            var conversation = await _db.ChatConversations.FirstOrDefaultAsync(c => c.Id == targetConvId);
            if (conversation == null) continue;

            if (conversation.IsGroup)
            {
                var isParticipant = await _db.ConversationParticipants
                    .AnyAsync(p => p.ConversationId == targetConvId && p.UserId == userId);
                if (!isParticipant) continue;
            }
            else
            {
                // For DMs, the user must be the initiator or participant
                var isDmParticipant = conversation.InitiatorId == userId || conversation.ParticipantId == userId;
                if (!isDmParticipant) continue;
            }

            conversation.LastMessageAt = DateTime.UtcNow;

            var forwardedMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = targetConvId,
                SenderId = userId,
                Content = originalMessage.Content,
                AttachmentUrl = originalMessage.AttachmentUrl,
                AttachmentType = originalMessage.AttachmentType,
                Status = MessageStatus.Sent,
                SentAt = DateTime.UtcNow,
                IsForwarded = true
            };

            _db.ChatMessages.Add(forwardedMessage);
            await _db.SaveChangesAsync();

            var savedMessage = await _db.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.RepliedToMessage)
                    .ThenInclude(rm => rm!.Sender)
                .Include(m => m.Reactions)
                    .ThenInclude(r => r!.User)
                .FirstOrDefaultAsync(m => m.Id == forwardedMessage.Id);

            if (savedMessage != null)
            {
                results.Add(MapToDto(savedMessage, userId));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetConversationParticipantUserIdsAsync(Guid conversationId)
    {
        var conversation = await _db.ChatConversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null) return new List<Guid>();

        if (conversation.IsGroup)
        {
            return conversation.Participants.Select(p => p.UserId).ToList();
        }
        else
        {
            var list = new List<Guid> { conversation.InitiatorId };
            if (conversation.ParticipantId.HasValue)
            {
                list.Add(conversation.ParticipantId.Value);
            }
            return list;
        }
    }
}
