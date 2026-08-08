using FluentAssertions;
using SHIELDON.API;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SHIELDON.Tests.Integration;

public class MonitoringIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public MonitoringIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessHeartbeat_UnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/monitoring/tutor/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
