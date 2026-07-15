using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Features.Chat.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Enums;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using System.Security.Claims;

namespace SHIELDON.API.Hubs;

/// <summary>
/// SignalR Hub for real-time chat, delivery receipts, and WebRTC signaling.
///
/// Connection Model:
/// - Each connected user joins a personal group named after their User ID string.
/// - When user A sends a message to user B, the hub persists it via ChatService,
///   then broadcasts it to both users' personal groups so both see it instantly.
/// - Group messages are broadcast to the conversation's group (keyed by ConversationId).
///
/// JWT Auth:
/// - SignalR passes the JWT as a query string param (?access_token=...) because
///   browsers cannot set Authorization headers on WebSocket connections.
///   The OnMessageReceived event in Program.cs reads it from the query string.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly PresenceTracker _presenceTracker;
    private readonly AppDbContext _db;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, PresenceTracker presenceTracker, AppDbContext db, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _presenceTracker = presenceTracker;
        _db = db;
        _logger = logger;
    }

    // ── Connection Lifecycle ───────────────────────────────────────────────────

    /// <summary>
    /// Called when a client connects. Adds the user to their personal group for targeted delivery.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());

            bool isOnline = await _presenceTracker.UserConnected(userId, Context.ConnectionId);
            if (isOnline)
            {
                await Clients.Others.SendAsync("UserIsOnline", userId.ToString());
                try
                {
                    await MarkIncomingSentMessagesAsDelivered(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error automatically marking incoming messages as Delivered for connected user {UserId}.", userId);
                }
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId != Guid.Empty)
        {
            bool isOffline = await _presenceTracker.UserDisconnected(userId, Context.ConnectionId);
            if (isOffline)
            {
                var user = await _db.Users.FindAsync(userId);
                if (user != null)
                {
                    user.LastSeenAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                await Clients.Others.SendAsync("UserIsOffline", userId.ToString(), DateTime.UtcNow);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    // ── Messaging ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a direct message. Persists it and broadcasts to sender + recipient.
    /// After sending, notifies the recipient to mark previous messages as Delivered.
    /// </summary>
    public async Task SendMessage(SendMessageRequest request)
    {
        var senderId = GetCurrentUserId();
        if (senderId == Guid.Empty) return;

        // Guard: must have either text content or an attachment
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl)) return;

        try
        {
            var messageDto = await _chatService.SendMessageAsync(senderId, request);

            // Deliver to recipient
            await Clients.Group(request.RecipientId.ToString()).SendAsync("ReceiveMessage", messageDto);

            // Echo to sender's own group (handles multiple tabs)
            await Clients.Group(senderId.ToString()).SendAsync("ReceiveMessage", messageDto);

            // Notify recipient to mark all previously Sent messages from this sender as Delivered
            await Clients.Group(request.RecipientId.ToString())
                .SendAsync("MessagesDelivered", senderId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessage failed. SenderId={SenderId}, RecipientId={RecipientId}, Content='{Content}', AttachmentUrl='{AttachmentUrl}', AttachmentType={AttachmentType}",
                senderId, request.RecipientId, request.Content, request.AttachmentUrl, request.AttachmentType);
            throw; // Re-throw so SignalR propagates the error back to the client
        }
    }

    /// <summary>
    /// Sends a text message to a group conversation and broadcasts it to all participants.
    /// </summary>
    public async Task SendGroupMessage(SendGroupMessageRequest request)
    {
        var senderId = GetCurrentUserId();
        if (senderId == Guid.Empty) return;

        // Guard: must have either text content or an attachment
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl)) return;

        try
        {
            var messageDto = await _chatService.SendGroupMessageAsync(senderId, request);

            var participantIds = await _db.ConversationParticipants
                .Where(p => p.ConversationId == request.ConversationId)
                .Select(p => p.UserId)
                .ToListAsync();

            foreach (var pId in participantIds)
            {
                var recipientDto = new ChatMessageDto
                {
                    Id = messageDto.Id,
                    ConversationId = messageDto.ConversationId,
                    SenderId = messageDto.SenderId,
                    SenderName = messageDto.SenderName,
                    SenderAvatarUrl = messageDto.SenderAvatarUrl,
                    Content = messageDto.Content,
                    Status = messageDto.Status,
                    AttachmentType = messageDto.AttachmentType,
                    AttachmentUrl = messageDto.AttachmentUrl,
                    SentAt = messageDto.SentAt,
                    IsOwnMessage = messageDto.SenderId == pId,
                    IsDeleted = messageDto.IsDeleted,
                    IsForwarded = messageDto.IsForwarded,
                    RepliedToMessageId = messageDto.RepliedToMessageId,
                    RepliedToMessageContent = messageDto.RepliedToMessageContent,
                    RepliedToMessageSenderName = messageDto.RepliedToMessageSenderName,
                    RepliedToMessageAttachmentType = messageDto.RepliedToMessageAttachmentType,
                    Reactions = messageDto.Reactions
                };
                await Clients.Group(pId.ToString()).SendAsync("ReceiveMessage", recipientDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGroupMessage failed. SenderId={SenderId}, ConversationId={ConversationId}, Content='{Content}', AttachmentUrl='{AttachmentUrl}', AttachmentType={AttachmentType}",
                senderId, request.ConversationId, request.Content, request.AttachmentUrl, request.AttachmentType);
            throw;
        }
    }

    /// <summary>
    /// Sends a message with an attachment URL (voice note, image, or document).
    /// The client must first upload the file via POST /api/chat/upload to obtain the URL.
    /// </summary>
    public async Task SendAttachmentMessage(
        Guid recipientId,
        string content,
        string attachmentUrl,
        AttachmentType attachmentType)
    {
        var senderId = GetCurrentUserId();
        if (senderId == Guid.Empty) return;

        var request = new SendMessageRequest 
        { 
            RecipientId = recipientId, 
            Content = content,
            AttachmentUrl = attachmentUrl,
            AttachmentType = attachmentType
        };
        
        await SendMessage(request);
    }

    // ── Delivery Receipts ──────────────────────────────────────────────────────

    /// <summary>
    /// Marks messages from a sender as Delivered. Called by the recipient when they come online.
    /// Notifies the original sender of the status change.
    /// </summary>
    public async Task MarkAsDelivered(Guid senderId, Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return;

        await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && m.SenderId == senderId
                     && m.Status == MessageStatus.Sent)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, MessageStatus.Delivered));

        // Notify the original sender their messages were delivered
        await Clients.Group(senderId.ToString())
            .SendAsync("MessageStatusChanged", new
            {
                conversationId,
                Status = (int)MessageStatus.Delivered,
                UpdatedByUserId = currentUserId
            });
    }

    /// <summary>
    /// Marks all messages in a conversation as Read. Called when the recipient opens the chat.
    /// Notifies the original sender(s) of the status change.
    /// </summary>
    public async Task MarkAsRead(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return;

        // Find all unread senders before updating
        var senderIds = await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && m.SenderId != currentUserId
                     && m.Status != MessageStatus.Read)
            .Select(m => m.SenderId)
            .Distinct()
            .ToListAsync();

        await _db.ChatMessages
            .Where(m => m.ConversationId == conversationId
                     && m.SenderId != currentUserId
                     && m.Status != MessageStatus.Read)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, MessageStatus.Read));

        // Notify each original sender their messages were read
        foreach (var sid in senderIds)
        {
            await Clients.Group(sid.ToString())
                .SendAsync("MessageStatusChanged", new
                {
                    conversationId,
                    Status = (int)MessageStatus.Read,
                    UpdatedByUserId = currentUserId
                });
        }
    }

    // ── WebRTC Signaling (1-on-1 Calls) ───────────────────────────────────────

    /// <summary>Relays a WebRTC SDP offer from caller to target user.</summary>
    public async Task SendCallOffer(WebRtcSignalDto dto)
    {
        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty) return;

        await Clients.Group(dto.TargetUserId.ToString())
            .SendAsync("CallIncoming", new { CallerId = callerId, dto.Signal });
    }

    /// <summary>Relays a WebRTC SDP answer from the receiver back to the caller.</summary>
    public async Task SendCallAnswer(WebRtcSignalDto dto)
    {
        var answererId = GetCurrentUserId();
        if (answererId == Guid.Empty) return;

        await Clients.Group(dto.TargetUserId.ToString())
            .SendAsync("CallAnswered", new { AnswererId = answererId, dto.Signal });
    }

    /// <summary>Relays an ICE candidate for NAT traversal between peers.</summary>
    public async Task SendIceCandidate(WebRtcSignalDto dto)
    {
        var senderId = GetCurrentUserId();
        if (senderId == Guid.Empty) return;

        await Clients.Group(dto.TargetUserId.ToString())
            .SendAsync("IceCandidateReceived", new { SenderId = senderId, dto.Signal });
    }

    /// <summary>Signals the remote peer that the call has been ended or rejected.</summary>
    public async Task EndCall(Guid targetUserId)
    {
        var callerId = GetCurrentUserId();
        if (callerId == Guid.Empty) return;

        await Clients.Group(targetUserId.ToString())
            .SendAsync("CallEnded", callerId.ToString());
    }

    // ── Presence & Typing ─────────────────────────────────────────────────────

    public async Task<Guid[]> GetOnlineUsers()
    {
        return await _presenceTracker.GetOnlineUsers();
    }

    /// <summary>Broadcasts a typing indicator in a conversation (DM or Group) to other participants.</summary>
    public async Task NotifyTyping(Guid conversationId)
    {
        var senderId = GetCurrentUserId();
        if (senderId == Guid.Empty) return;

        var conversation = await _db.ChatConversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null) return;

        // Resolve sender name from loaded participants (avoids extra DB round-trip)
        var senderParticipant = conversation.Participants.FirstOrDefault(p => p.UserId == senderId);
        var senderName = senderParticipant?.User?.FullName
            ?? (await _db.Users.Where(u => u.Id == senderId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefaultAsync())
            ?? "Someone";

        if (conversation.IsGroup)
        {
            var otherParticipants = conversation.Participants
                .Where(p => p.UserId != senderId)
                .ToList();

            foreach (var p in otherParticipants)
            {
                await Clients.Group(p.UserId.ToString())
                             .SendAsync("UserIsTyping", conversationId.ToString(), senderId.ToString(), senderName);
            }
        }
        else
        {
            var otherUserId = conversation.InitiatorId == senderId
                ? conversation.ParticipantId
                : (Guid?)conversation.InitiatorId;

            if (otherUserId.HasValue)
            {
                await Clients.Group(otherUserId.Value.ToString())
                             .SendAsync("UserIsTyping", conversationId.ToString(), senderId.ToString(), senderName);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                 ?? Context.User?.FindFirst("sub");

        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }

    private async Task MarkIncomingSentMessagesAsDelivered(Guid recipientId)
    {
        var sentMessagesInfo = await _db.ChatMessages
            .Include(m => m.Conversation)
            .Where(m => !m.Conversation.IsGroup 
                     && (m.Conversation.InitiatorId == recipientId || m.Conversation.ParticipantId == recipientId)
                     && m.SenderId != recipientId
                     && m.Status == MessageStatus.Sent)
            .Select(m => new { m.ConversationId, m.SenderId })
            .Distinct()
            .ToListAsync();

        if (sentMessagesInfo.Count == 0) return;

        foreach (var info in sentMessagesInfo)
        {
            // Update in DB
            await _db.ChatMessages
                .Where(m => m.ConversationId == info.ConversationId
                         && m.SenderId == info.SenderId
                         && m.Status == MessageStatus.Sent)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, MessageStatus.Delivered));

            // Notify the original sender
            await Clients.Group(info.SenderId.ToString())
                .SendAsync("MessageStatusChanged", new
                {
                    conversationId = info.ConversationId,
                    Status = (int)MessageStatus.Delivered,
                    UpdatedByUserId = recipientId
                });
        }
    }
}
