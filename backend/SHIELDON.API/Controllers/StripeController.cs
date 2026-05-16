using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHIELDON.Application.Features.Payment.Interfaces;

namespace SHIELDON.API.Controllers;

[ApiController]
[Route("api/webhooks/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public StripeController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Webhook endpoint called by Stripe when events (like checkout.session.completed) occur.
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // Stripe must be able to hit this without authentication
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);

        // Stripe sends the signature in this header
        var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signatureHeader))
        {
            return BadRequest("Missing Stripe signature header.");
        }

        try
        {
            await _paymentService.HandleWebhookAsync(json, signatureHeader, ct);
            return Ok(); // Acknowledge receipt to Stripe
        }
        catch (Exception ex)
        {
            // Logged inside the service, just return 400 so Stripe knows it failed
            return BadRequest(new { Error = ex.Message });
        }
    }
}
