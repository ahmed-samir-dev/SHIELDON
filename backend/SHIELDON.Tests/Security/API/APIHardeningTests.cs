using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SHIELDON.Tests.Integration;
using SHIELDON.API;

namespace SHIELDON.Tests.Security.API;

/// <summary>
/// Integration tests verifying HTTP security headers, CORS behavior, and rate-limit guards.
/// These tests use the CustomWebApplicationFactory to spin up the full API pipeline
/// and verify security headers are present and cross-origin requests are properly handled.
/// </summary>
public class APIHardeningTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public APIHardeningTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ─── Security Headers: X-Content-Type-Options ────────────────────────────

    [Fact]
    public async Task Response_ShouldInclude_XContentTypeOptions_NoSniff()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.Headers.TryGetValues("X-Content-Type-Options", out var values);
        var header = values?.FirstOrDefault();
        header.Should().Be("nosniff",
            "X-Content-Type-Options: nosniff must be present to prevent MIME sniffing attacks");
    }

    // ─── Security Headers: X-Frame-Options ───────────────────────────────────

    [Fact]
    public async Task Response_ShouldInclude_XFrameOptions()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        var hasXFrame = response.Headers.Contains("X-Frame-Options");
        hasXFrame.Should().BeTrue(
            "X-Frame-Options header must be present to prevent clickjacking via iframes");
    }

    // ─── Security Headers: No Server Header Leak ─────────────────────────────

    [Fact]
    public async Task Response_ShouldNot_LeakServerHeader()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert - Server header reveals infrastructure info and should be absent or minimal
        var serverHeader = response.Headers.Server?.ToString();
        if (serverHeader != null)
        {
            serverHeader.Should().NotContain("Kestrel",
                "Kestrel version in Server header leaks infrastructure info");
            serverHeader.Should().NotContain("Microsoft",
                "Microsoft in Server header leaks infrastructure info");
        }
    }

    // ─── Unauthenticated Access to Protected Endpoints ───────────────────────

    [Theory]
    [InlineData("/api/courses")]
    [InlineData("/api/profile")]
    [InlineData("/api/monitoring/tutor/dashboard")]
    [InlineData("/api/chat/inbox")]
    [InlineData("/api/notifications")]
    public async Task ProtectedEndpoints_WithoutAuth_ShouldReturn401(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{endpoint} must return 401 for unauthenticated requests");
    }

    // ─── CORS: Missing Origin ─────────────────────────────────────────────────

    [Fact]
    public async Task Request_WithoutOrigin_ShouldNotExposeCORSAllowAll()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        // No Origin header

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Access-Control-Allow-Origin: * must NOT be present (overly permissive CORS)
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var corsValues);
        var corsHeader = corsValues?.FirstOrDefault();
        corsHeader.Should().NotBe("*",
            "wildcard CORS (Access-Control-Allow-Origin: *) must not be returned on the main API - only specific origins should be whitelisted");
    }

    // ─── CORS: Non-Whitelisted Origin Preflight ───────────────────────────────

    [Fact]
    public async Task Preflight_FromNonWhitelistedOrigin_ShouldNotAllowCredentials()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.TryAddWithoutValidation("Origin", "https://evil-attacker.com");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "Authorization");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - non-whitelisted origins must not receive Access-Control-Allow-Credentials: true
        response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credValues);
        var allowCreds = credValues?.FirstOrDefault();
        allowCreds.Should().NotBe("true",
            "non-whitelisted origins must not be allowed with credentials via CORS");
    }

    // ─── Method Not Allowed on Auth Endpoints ────────────────────────────────

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/register")]
    public async Task AuthEndpoints_WithGetMethod_ShouldReturn405(string endpoint)
    {
        // Act - GET on POST-only auth endpoints
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
            $"GET method must not be allowed on POST-only endpoint {endpoint}");
    }
}
