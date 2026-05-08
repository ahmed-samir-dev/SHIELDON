namespace SHIELDON.Application.Features.AI.DTOs;

/// <summary>
/// Incoming request body from the Angular frontend.
/// History contains prior turns so the model maintains conversation context.
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItem> History { get; set; } = [];
}

/// <summary>
/// One turn in the conversation history as serialised by the frontend.
/// Role is "user" or "model" (Gemini convention).
/// </summary>
public class ChatHistoryItem
{
    public string Role    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response returned to the Angular frontend.
/// </summary>
public class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
}
