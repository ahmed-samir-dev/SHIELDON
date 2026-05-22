using System.Net.Http.Json;
using SHIELDON.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace SHIELDON.Infrastructure.Services;

public class LingvaTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LingvaTranslationService> _logger;

    public LingvaTranslationService(HttpClient httpClient, ILogger<LingvaTranslationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        // Public instance of Lingva
        _httpClient.BaseAddress = new Uri("https://lingva.ml/api/v1/");
    }

    public async Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "en", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        try
        {
            var encodedText = Uri.EscapeDataString(text);
            var url = $"{sourceLanguage}/{targetLanguage}/{encodedText}";
            
            var response = await _httpClient.GetFromJsonAsync<LingvaResponse>(url, cancellationToken);
            
            if (response != null && !string.IsNullOrEmpty(response.Translation))
            {
                return response.Translation;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lingva API translation failed for text: {Text}", text);
        }

        // Fallback to original text if translation fails
        return text;
    }

    private class LingvaResponse
    {
        [JsonPropertyName("translation")]
        public string? Translation { get; set; }
    }
}
