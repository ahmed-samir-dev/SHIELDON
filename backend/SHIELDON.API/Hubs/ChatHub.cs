using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SHIELDON.Application.Features.Chat.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Infrastructure.Services;
using System.Security.Claims;

namespace SHIELDON.API.Hubs;

/// <summary>
/// SignalR Hub for real-time chat.
///
/// Connection Model:
/// - Each connected user joins a personal "group" named after their User ID string.
/// - When user A sends a message to user B, the hub persists it via ChatService,
///   then broadcasts it to both users' groups so both see it instantly.
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

    public ChatHub(IChatService chatService, PresenceTracker presenceTracker)
    {
        _chatService = chatService;
        _presenceTracker = presenceTracker;
    }

    /// <summary>
    /// Called automatically when a client connects.
    /// Adds the user to their personal group so they receive messages addressed to them.
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
                await Clients.Others.SendAsync("UserIsOffline", userId.ToString());
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client-callable method: Send a chat message to a recipient.
    /// 1. Persists the message to the database via ChatService.
    /// 2. Broadcasts the message to both the sender's and recipient's personal groups.
    /// </summary>
    /// <param name="request">Contains RecipientId and Content.</param>
    public async Task SendMessage(SendMessageRequest request)
    {
        var senderId = GetCurrentUserId();
        if (senderId == Guid.Empty) return;

        // Trim and validate content
        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 2000)
            return;

        // Persist to database
        var messageDto = await _chatService.SendMessageAsync(senderId, request);

        // Broadcast to the recipient's personal group
        await Clients
            .Group(request.RecipientId.ToString())
            .SendAsync("ReceiveMessage", messageDto);

        // Echo back to the sender's own group (handles multiple browser tabs)
        await Clients
            .Group(senderId.ToString())
            .SendAsync("ReceiveMessage", messageDto);
    }

    public async Task<Guid[]> GetOnlineUsers()
    {
        return await _presenceTracker.GetOnlineUsers();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)
                 ?? Context.User?.FindFirst("sub");

        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }
}
