using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SHIELDON.Application.Common;
using SHIELDON.Application.Features.AI.DTOs;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Stage 6.1 — SHIELDON AI Assistant (backend proxy).
///
/// POST /api/ai/chat — Proxies a chat message to the Gemini API.
///   All roles: Admin, Tutor, Student (when NOT in an active exam session).
///   The Gemini API key never reaches the frontend — it lives server-side only.
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _ai;

    public AIController(IAIService ai)
    {
        _ai = ai;
    }

    /// <summary>
    /// Accepts a user message + conversation history, proxies to Gemini, returns AI reply.
    /// </summary>
    /// <param name="request">Message text + prior chat history for context.</param>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiResponse<ChatResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),       StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),       StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(ApiResponse<object>.Fail("Message cannot be empty."));

        // Map incoming DTO history to the interface's ChatTurn records
        var history = request.History
            .Select(h => new ChatTurn(h.Role, h.Content))
            .ToList();

        var reply = await _ai.ChatAsync(request.Message.Trim(), history);
        return Ok(ApiResponse<ChatResponse>.Ok(new ChatResponse { Reply = reply }));
    }
}
