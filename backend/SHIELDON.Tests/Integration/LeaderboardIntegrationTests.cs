using FluentAssertions;
using SHIELDON.API;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SHIELDON.Tests.Integration;

public class LeaderboardIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public LeaderboardIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCourseLeaderboard_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/courses/{Guid.NewGuid()}/leaderboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
