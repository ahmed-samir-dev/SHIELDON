using Xunit;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SHIELDON.Tests.Integration;
using SHIELDON.API;

namespace SHIELDON.Tests.Security.Session;

/// <summary>
/// Integration security tests for the SecurityHub SignalR endpoint.
/// SecurityHub uses [Authorize], so unauthenticated WebSocket/SignalR connections must be rejected.
/// Full two-device displacement testing requires a live SignalR client and is documented as
/// a manual verification step; these tests verify the HTTP-level endpoint security.
/// </summary>
public class SecurityHubTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityHubTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ─── Unauthenticated GET /hubs/security → 401 ────────────────────────────

    [Fact]
    public async Task SecurityHub_WithoutToken_ShouldReturn401()
    {
        // Arrange - SignalR negotiate endpoint is at /hubs/security/negotiate
        var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/security/negotiate?negotiateVersion=1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - [Authorize] on SecurityHub must reject unauthenticated negotiate requests
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "SecurityHub requires authentication - unauthenticated negotiate requests must return 401");
    }

    // ─── Unauthenticated LeaderboardHub Negotiate → 401 ─────────────────────

    [Fact]
    public async Task LeaderboardHub_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/leaderboard/negotiate?negotiateVersion=1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "LeaderboardHub requires authentication - unauthenticated negotiate requests must return 401");
    }

    // ─── Unauthenticated ChatHub Negotiate → 401 ─────────────────────────────

    [Fact]
    public async Task ChatHub_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/chat/negotiate?negotiateVersion=1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "ChatHub requires authentication - unauthenticated negotiate requests must return 401");
    }

    // ─── Unauthenticated AttendanceHub Negotiate → 401 ───────────────────────

    [Fact]
    public async Task AttendanceHub_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/attendance/negotiate?negotiateVersion=1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "AttendanceHub requires authentication - unauthenticated negotiate requests must return 401");
    }

    // ─── Unauthenticated DashboardHub Negotiate → 401 ────────────────────────

    [Fact]
    public async Task DashboardHub_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/dashboard/negotiate?negotiateVersion=1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "DashboardHub requires authentication - unauthenticated negotiate requests must return 401");
    }
}
