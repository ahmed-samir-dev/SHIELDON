using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace SHIELDON.Tests.Scratch;

class TestAIProgram {
    static async Task Main() {
        var _apiKey = "YOUR_GEMINI_API_KEY";
        var Model = "gemini-flash-latest";
        var BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
        var SystemInstruction = "You are SHIELDON Assistant...";
        
        var requestBody = new {
            systemInstruction = new { parts = new[] { new { text = SystemInstruction } } },
            contents = new[] {
                new { role = "user", parts = new[] { new { text = "Hello" } } }
            },
            generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var _http = new HttpClient();
        
        try {
            var url = $"{BaseUrl}/{Model}:generateContent?key={_apiKey}";
            var response = await _http.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine((int)response.StatusCode);
            Console.WriteLine(responseText);
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }
}
