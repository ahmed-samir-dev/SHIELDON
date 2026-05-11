using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SHIELDON.Application.Interfaces;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Proxies chat requests to the Google Gemini API (gemini-2.0-flash model).
/// The API key is read from IConfiguration (appsettings.Development.json, gitignored).
/// The key is NEVER sent to or stored on the frontend.
/// </summary>
public class AIService : IAIService
{
    private readonly HttpClient    _http;
    private readonly string        _apiKey;
    private const   string         Model    = "gemini-flash-latest";
    private const   string         BaseUrl  = "https://generativelanguage.googleapis.com/v1beta/models";

    // System instruction injected into every conversation so Gemini knows its context.
    private const string SystemInstruction =
        "You are SHIELDON Assistant, an AI helper embedded in the SHIELDON Examination Management " +
        "Platform. You help students, tutors, and administrators with questions about exams, courses, " +
        "grades, assignments, and platform features. Be concise, professional, and helpful. " +
        "If you do not know something, say so honestly.";

    public AIService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _http   = httpClientFactory.CreateClient("Gemini");
        _apiKey = configuration["GeminiSettings:ApiKey"]
                  ?? throw new InvalidOperationException(
                      "GeminiSettings:ApiKey is not configured. " +
                      "Add it to appsettings.Development.json (gitignored).");
    }

    /// <inheritdoc/>
    public async Task<string> ChatAsync(string message, IEnumerable<ChatTurn> history)
    {
        // Build the Gemini contents array from conversation history + new user message
        var contents = new List<object>();

        foreach (var turn in history)
        {
            contents.Add(new
            {
                role  = turn.Role,   // "user" or "model"
                parts = new[] { new { text = turn.Content } }
            });
        }

        // Append the latest user message
        contents.Add(new
        {
            role  = "user",
            parts = new[] { new { text = message } }
        });

        var requestBody = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = SystemInstruction } }
            },
            contents,
            generationConfig = new
            {
                temperature     = 0.7,
                maxOutputTokens = 1024
            }
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url      = $"{BaseUrl}/{Model}:generateContent?key={_apiKey}";
        var response = await _http.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Gemini API error {(int)response.StatusCode}: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc    = JsonDocument.Parse(responseJson);

        // Navigate: candidates[0].content.parts[0].text
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "I'm sorry, I couldn't generate a response. Please try again.";
    }
}
