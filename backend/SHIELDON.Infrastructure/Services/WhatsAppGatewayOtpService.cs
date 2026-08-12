using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Exceptions;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Delivers OTP codes via a self-hosted WhatsApp Gateway microservice (Node.js + Baileys).
/// The gateway uses WhatsApp Web socket protocol to send real WhatsApp messages - no Twilio, no per-message cost.
/// Code generation, hashing, and verification are fully handled by ProfileService in C#.
/// </summary>
public class WhatsAppGatewayOtpService : IOtpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppGatewayOtpService> _logger;

    /// <summary>REST endpoint exposed by the WhatsApp Gateway microservice.</summary>
    private const string SEND_OTP_ENDPOINT = "/api/send-otp";

    public WhatsAppGatewayOtpService(HttpClient httpClient, ILogger<WhatsAppGatewayOtpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Posts the phone number and pre-generated OTP code to the WhatsApp Gateway,
    /// which delivers a formatted WhatsApp message to the user.
    /// </summary>
    public async Task SendOtpAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Sending WhatsApp OTP to {PhoneNumber} via self-hosted gateway", phoneNumber);

            var payload = new { phone = phoneNumber, code };
            var response = await _httpClient.PostAsJsonAsync(SEND_OTP_ENDPOINT, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("WhatsApp Gateway returned {StatusCode}: {Error}", response.StatusCode, errorBody);
                throw new BusinessRuleException("Failed to deliver WhatsApp OTP. Please try again.");
            }

            _logger.LogInformation("WhatsApp OTP delivered successfully to {PhoneNumber}", phoneNumber);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot reach WhatsApp Gateway at {BaseAddress}", _httpClient.BaseAddress);
            throw new BusinessRuleException("WhatsApp delivery service is temporarily unavailable. Please try again shortly.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "WhatsApp Gateway request timed out for {PhoneNumber}", phoneNumber);
            throw new BusinessRuleException("WhatsApp delivery timed out. Please try again.");
        }
        catch (Exception ex) when (ex is not BusinessRuleException)
        {
            _logger.LogError(ex, "Unexpected error sending WhatsApp OTP to {PhoneNumber}", phoneNumber);
            throw new BusinessRuleException("An unexpected error occurred while sending OTP. Please try again.");
        }
    }
}
