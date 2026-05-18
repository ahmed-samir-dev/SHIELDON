using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Features.Payment.Interfaces;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/webhooks/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<StripeController> _logger;

    public StripeController(IPaymentService paymentService, ILogger<StripeController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint called by Stripe when events (like checkout.session.completed) occur.
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Stripe must be able to hit this without authentication
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        // Enable buffering so the body can be re-read if needed
        Request.EnableBuffering();

        string json;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            json = await reader.ReadToEndAsync(ct);
        }

        _logger.LogInformation("[STRIPE WEBHOOK] Received request. Body length: {Length}", json.Length);

        // Stripe sends the signature in this header
        var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signatureHeader))
        {
            _logger.LogWarning("[STRIPE WEBHOOK] Missing Stripe-Signature header.");
            return BadRequest("Missing Stripe signature header.");
        }

        try
        {
            await _paymentService.HandleWebhookAsync(json, signatureHeader, ct);
            _logger.LogInformation("[STRIPE WEBHOOK] ✅ Event processed successfully.");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[STRIPE WEBHOOK] ❌ Failed to process webhook: {Message}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
    }
}
