using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SHIELDON.API.Hubs;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Chat.DTOs;
using SHIELDON.Application.Interfaces;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Handles REST endpoints for chat: inbox, message history, groups, and file attachments.
/// Real-time message sending and WebRTC signaling are handled by ChatHub (SignalR), not here.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IChatService chatService, IFileStorageService fileStorageService, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _fileStorageService = fileStorageService;
        _hubContext = hubContext;
    }

    // ── GET /api/chat/inbox ──────────────────────────────────────────────────
    /// <summary>Returns all conversations (DMs and groups) for the current user, sorted by most recent activity.</summary>
    [HttpGet("inbox")]
    [ProducesResponseType(typeof(ApiResponse<List<ConversationSummaryDto>>), 200)]
    public async Task<IActionResult> GetInbox()
    {
        var userId = GetCurrentUserId();
        var inbox = await _chatService.GetInboxAsync(userId);
        return Ok(ApiResponse<List<ConversationSummaryDto>>.Ok(inbox));
    }

    // ── GET /api/chat/conversations/{conversationId}/messages ────────────────
    /// <summary>Returns full message history for a conversation. Also marks received messages as Read.</summary>
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
    /// <summary>Returns all users the current user can start a chat with, including online status.</summary>
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
    /// Returns the existing DM conversation ID between the current user and a recipient.
    /// Returns null data if no DM conversation exists yet.
    /// </summary>
    [HttpGet("conversation-id")]
    [ProducesResponseType(typeof(ApiResponse<Guid?>), 200)]
    public async Task<IActionResult> GetConversationId([FromQuery] Guid recipientId)
    {
        var userId = GetCurrentUserId();
        var convId = await _chatService.GetConversationIdAsync(userId, recipientId);
        return Ok(ApiResponse<Guid?>.Ok(convId));
    }

    // ── POST /api/chat/group ─────────────────────────────────────────────────
    /// <summary>
    /// Creates a new group conversation. Restricted to Admin and Tutor roles only.
    /// The authenticated user is automatically added as the group admin.
    /// </summary>
    [HttpPost("group")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSummaryDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.Fail("Invalid request data."));

        var userId = GetCurrentUserId();
        var group = await _chatService.CreateGroupAsync(userId, request);
        return CreatedAtAction(nameof(GetInbox), ApiResponse<ConversationSummaryDto>.Ok(group));
    }

    // ── GET /api/chat/group/{conversationId}/participants ────────────────────
    /// <summary>Returns the participant list for a group conversation.</summary>
    [HttpGet("group/{conversationId:guid}/participants")]
    [ProducesResponseType(typeof(ApiResponse<List<GroupParticipantDto>>), 200)]
    public async Task<IActionResult> GetGroupParticipants(Guid conversationId)
    {
        var participants = await _chatService.GetGroupParticipantsAsync(conversationId);
        return Ok(ApiResponse<List<GroupParticipantDto>>.Ok(participants));
    }

    // ── PUT /api/chat/group/{conversationId}/rename ─────────────────────────
    /// <summary>Renames a group conversation. Restricted to group admin.</summary>
    [HttpPut("group/{conversationId:guid}/rename")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSummaryDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RenameGroup(Guid conversationId, [FromBody] RenameGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewGroupName))
            return BadRequest(ApiResponse<object>.Fail("Group name cannot be empty."));

        try
        {
            var adminId = GetCurrentUserId();
            var summary = await _chatService.RenameGroupAsync(conversationId, adminId, request.NewGroupName);
            
            // Broadcast the new name to all group participants
            var participants = await _chatService.GetGroupParticipantsAsync(conversationId);
            foreach (var participant in participants)
            {
                await _hubContext.Clients.Group(participant.UserId.ToString())
                    .SendAsync("GroupRenamed", conversationId, request.NewGroupName);
            }
                
            return Ok(ApiResponse<ConversationSummaryDto>.Ok(summary));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── POST /api/chat/group/{conversationId}/members ────────────────────────
    /// <summary>Adds new members to the group. Restricted to group admin.</summary>
    [HttpPost("group/{conversationId:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AddGroupMembers(Guid conversationId, [FromBody] AddGroupMembersRequest request)
    {
        if (request.MemberIds == null || !request.MemberIds.Any())
            return BadRequest(ApiResponse<object>.Fail("No members specified."));

        try
        {
            var adminId = GetCurrentUserId();
            await _chatService.AddGroupMembersAsync(conversationId, adminId, request.MemberIds);
            
            // Note: In a complete implementation, we'd also notify the new members' personal groups
            // so the conversation appears in their inbox immediately.
            foreach (var newMemberId in request.MemberIds)
            {
                await _hubContext.Clients.Group(newMemberId.ToString())
                    .SendAsync("AddedToGroup", conversationId);
            }
            // And notify all group members that the participant list changed
            var participants = await _chatService.GetGroupParticipantsAsync(conversationId);
            foreach (var participant in participants)
            {
                await _hubContext.Clients.Group(participant.UserId.ToString())
                    .SendAsync("GroupParticipantsChanged", conversationId);
            }
 
            return Ok(ApiResponse<object>.Ok(new { Message = "Members added successfully." }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── DELETE /api/chat/group/{conversationId}/members/{userId} ─────────────
    /// <summary>Removes a member from the group. Restricted to group admin.</summary>
    [HttpDelete("group/{conversationId:guid}/members/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RemoveGroupMember(Guid conversationId, Guid userId)
    {
        try
        {
            var adminId = GetCurrentUserId();
            await _chatService.RemoveGroupMemberAsync(conversationId, adminId, userId);
            
            // Notify the removed member
            await _hubContext.Clients.Group(userId.ToString())
                .SendAsync("RemovedFromGroup", conversationId);
                
            // Notify remaining group members
            var remainingParticipants = await _chatService.GetGroupParticipantsAsync(conversationId);
            foreach (var participant in remainingParticipants)
            {
                await _hubContext.Clients.Group(participant.UserId.ToString())
                    .SendAsync("GroupParticipantsChanged", conversationId);
            }
 
            return Ok(ApiResponse<object>.Ok(new { Message = "Member removed successfully." }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpDelete("group/{conversationId}")]
    public async Task<IActionResult> DeleteGroup(Guid conversationId)
    {
        try
        {
            var adminId = GetCurrentUserId();

            // Fetch participants before deleting so we can notify them
            var participants = await _chatService.GetGroupParticipantsAsync(conversationId);
            
            await _chatService.DeleteGroupAsync(conversationId, adminId);
            
            // Notify all participants
            foreach (var participant in participants)
            {
                await _hubContext.Clients.Group(participant.UserId.ToString())
                    .SendAsync("GroupDeleted", conversationId);
            }

            return Ok(ApiResponse<object>.Ok(new { Message = "Group deleted successfully." }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── POST /api/chat/upload ────────────────────────────────────────────────
    /// <summary>
    /// Uploads a chat attachment (voice note, image, or document).
    /// Max file size: 10MB. Accepted types: PDF, DOCX, PPTX, JPG, PNG, MP3, WAV, WEBM, OGG, M4A.
    /// Returns the relative URL and resolved AttachmentType for use in SendMessage.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<AttachmentUploadResponse>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _fileStorageService.SaveChatAttachmentAsync(
                stream,
                file.FileName,
                file.ContentType);

            return Ok(ApiResponse<AttachmentUploadResponse>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── POST /api/chat/messages/{messageId}/react ───────────────────────────
    /// <summary>Adds, updates, or toggles/removes a reaction on a message.</summary>
    [HttpPost("messages/{messageId}/react")]
    [ProducesResponseType(typeof(ApiResponse<List<MessageReactionDto>>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ReactToMessage(Guid messageId, [FromBody] ReactToMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Emoji))
            return BadRequest(ApiResponse<object>.Fail("Emoji is required."));

        try
        {
            var userId = GetCurrentUserId();
            var (conversationId, reactions) = await _chatService.ReactToMessageAsync(userId, messageId, request.Emoji);

            var userName = User.FindFirst("firstName")?.Value + " " + User.FindFirst("lastName")?.Value;
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "Someone";
            }

            var participantIds = await _chatService.GetConversationParticipantUserIdsAsync(conversationId);

            foreach (var pId in participantIds)
            {
                await _hubContext.Clients.Group(pId.ToString())
                    .SendAsync("MessageReactionChanged", conversationId, messageId, userId, userName, request.Emoji, reactions);
            }

            return Ok(ApiResponse<List<MessageReactionDto>>.Ok(reactions));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── DELETE /api/chat/messages/{messageId} ────────────────────────────────
    /// <summary>Deletes a message for everyone. Allowed for sender or group admin.</summary>
    [HttpDelete("messages/{messageId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var conversationId = await _chatService.DeleteMessageAsync(userId, messageId);

            var participantIds = await _chatService.GetConversationParticipantUserIdsAsync(conversationId);

            foreach (var pId in participantIds)
            {
                await _hubContext.Clients.Group(pId.ToString())
                    .SendAsync("MessageDeleted", conversationId, messageId);
            }

            return Ok(ApiResponse<object>.Ok(new { Message = "Message deleted successfully." }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── POST /api/chat/messages/forward ─────────────────────────────────────
    /// <summary>Forwards a message to one or more conversations.</summary>
    [HttpPost("messages/forward")]
    [ProducesResponseType(typeof(ApiResponse<List<ChatMessageDto>>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ForwardMessage([FromBody] ForwardMessageRequest request)
    {
        if (request.TargetConversationIds == null || !request.TargetConversationIds.Any())
            return BadRequest(ApiResponse<object>.Fail("No target conversations specified."));

        try
        {
            var userId = GetCurrentUserId();
            var forwardedMessages = await _chatService.ForwardMessageAsync(userId, request);

            foreach (var msgDto in forwardedMessages)
            {
                var participantIds = await _chatService.GetConversationParticipantUserIdsAsync(msgDto.ConversationId);
                foreach (var pId in participantIds)
                {
                    var recipientDto = new ChatMessageDto
                    {
                        Id = msgDto.Id,
                        ConversationId = msgDto.ConversationId,
                        SenderId = msgDto.SenderId,
                        SenderName = msgDto.SenderName,
                        SenderAvatarUrl = msgDto.SenderAvatarUrl,
                        Content = msgDto.Content,
                        Status = msgDto.Status,
                        AttachmentType = msgDto.AttachmentType,
                        AttachmentUrl = msgDto.AttachmentUrl,
                        SentAt = msgDto.SentAt,
                        IsOwnMessage = msgDto.SenderId == pId,
                        IsDeleted = msgDto.IsDeleted,
                        IsForwarded = msgDto.IsForwarded,
                        RepliedToMessageId = msgDto.RepliedToMessageId,
                        RepliedToMessageContent = msgDto.RepliedToMessageContent,
                        RepliedToMessageSenderName = msgDto.RepliedToMessageSenderName,
                        RepliedToMessageAttachmentType = msgDto.RepliedToMessageAttachmentType,
                        Reactions = msgDto.Reactions
                    };

                    await _hubContext.Clients.Group(pId.ToString())
                        .SendAsync("ReceiveMessage", recipientDto);
                }
            }

            return Ok(ApiResponse<List<ChatMessageDto>>.Ok(forwardedMessages));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");
        return Guid.Parse(claim!.Value);
    }
}
