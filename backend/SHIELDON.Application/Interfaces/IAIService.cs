namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Proxies chat messages to the Gemini API and returns AI-generated replies.
/// The Gemini API key is NEVER exposed to the frontend — all calls go through this service.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Sends a user message (with optional prior conversation history) to Gemini
    /// and returns the model's text reply.
    /// </summary>
    Task<string> ChatAsync(string message, IEnumerable<ChatTurn> history);
}

/// <summary>
/// Represents one completed turn in the conversation (one user message + one AI reply).
/// Sent from the frontend so the model maintains context.
/// </summary>
public record ChatTurn(string Role, string Content);
