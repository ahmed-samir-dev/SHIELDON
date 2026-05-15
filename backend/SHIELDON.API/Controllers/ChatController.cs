using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Chat.DTOs;
using SHIELDON.Application.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Handles REST endpoints for chat history and inbox loading.
/// Real-time message sending is handled by ChatHub (SignalR), not this controller.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    // ── GET /api/chat/inbox ──────────────────────────────────────────────────
    /// <summary>Returns all conversations for the current user, sorted by most recent activity.</summary>
    [HttpGet("inbox")]
    [ProducesResponseType(typeof(ApiResponse<List<ConversationSummaryDto>>), 200)]
    public async Task<IActionResult> GetInbox()
    {
        var userId = GetCurrentUserId();
        var inbox = await _chatService.GetInboxAsync(userId);
        return Ok(ApiResponse<List<ConversationSummaryDto>>.Ok(inbox));
    }

    // ── GET /api/chat/conversations/{conversationId}/messages ────────────────
    /// <summary>Returns full message history for a conversation. Also marks received messages as read.</summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(ApiResponse<List<ChatMessageDto>>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMessages(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        var messages = await _chatService.GetMessagesAsync(conversationId, userId);
        return Ok(ApiResponse<List<ChatMessageDto>>.Ok(messages));
    }

    // ── GET /api/chat/users ──────────────────────────────────────────────────
    /// <summary>Returns all users the current user can start a chat with.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<List<ChatUserDto>>), 200)]
    public async Task<IActionResult> GetChatUsers()
    {
        var userId = GetCurrentUserId();
        var users = await _chatService.GetUsersForChatAsync(userId);
        return Ok(ApiResponse<List<ChatUserDto>>.Ok(users));
    }

    // ── GET /api/chat/conversation-id?recipientId={guid} ────────────────────
    /// <summary>
    /// Returns the existing conversation ID between current user and a recipient.
    /// Returns null data if no conversation has started yet.
    /// </summary>
    [HttpGet("conversation-id")]
    [ProducesResponseType(typeof(ApiResponse<Guid?>), 200)]
    public async Task<IActionResult> GetConversationId([FromQuery] Guid recipientId)
    {
        var userId = GetCurrentUserId();
        var convId = await _chatService.GetConversationIdAsync(userId, recipientId);
        return Ok(ApiResponse<Guid?>.Ok(convId));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");
        return Guid.Parse(claim!.Value);
    }
}
