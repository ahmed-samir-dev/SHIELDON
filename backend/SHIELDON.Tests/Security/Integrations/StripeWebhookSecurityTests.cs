using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SHIELDON.Tests.Integration;
using SHIELDON.API;

namespace SHIELDON.Tests.Security.Integrations;

/// <summary>
/// Integration security tests for the Stripe webhook endpoint.
/// Tests that the webhook rejects requests without a valid Stripe-Signature header
/// and validates the controller-level guards before calling the service.
/// Note: Full signature verification requires the exact WebhookSecret from Stripe CLI,
/// so these tests verify the HTTP-level rejection guards.
/// </summary>
public class StripeWebhookSecurityTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StripeWebhookSecurityTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ─── POST without Stripe-Signature header → 400 ───────────────────────────

    [Fact]
    public async Task Webhook_WithoutStripeSignatureHeader_ShouldReturn400()
    {
        // Arrange
        var payload = "{\"type\":\"checkout.session.completed\",\"data\":{}}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act - no Stripe-Signature header
        var response = await _client.PostAsync("/api/webhooks/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Stripe webhook requests without a Stripe-Signature header must be rejected with 400");
    }

    // ─── POST with Invalid / Forged Stripe-Signature → 400 ───────────────────

    [Fact]
    public async Task Webhook_WithInvalidStripeSignature_ShouldReturn400()
    {
        // Arrange
        var payload = "{\"type\":\"checkout.session.completed\",\"data\":{}}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        content.Headers.TryAddWithoutValidation("Stripe-Signature",
            "t=1234567890,v1=fakesignaturethatdoesnotmatch");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature",
            "t=1234567890,v1=fakesignaturethatdoesnotmatch");

        // Act - invalid signature must be rejected by Stripe SDK's EventUtility.ConstructEvent
        var response = await _client.SendAsync(request);

        // Assert - Stripe SDK throws StripeException on bad signature → controller returns 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "forged Stripe-Signature values must be rejected with 400");
    }

    // ─── GET method not allowed ───────────────────────────────────────────────

    [Fact]
    public async Task Webhook_WithGetMethod_ShouldReturn405()
    {
        // Act
        var response = await _client.GetAsync("/api/webhooks/stripe");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
            "Stripe webhook endpoint only accepts POST requests");
    }

    // ─── Empty Body with Signature Header → 400 ──────────────────────────────

    [Fact]
    public async Task Webhook_WithEmptyBody_ShouldReturn400()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", "t=1234,v1=somesig");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - empty body with signature should fail validation (either 400 from controller or from Stripe SDK)
        ((int)response.StatusCode).Should().BeInRange(400, 499,
            "empty webhook body with a fake signature must be rejected");
    }
}
