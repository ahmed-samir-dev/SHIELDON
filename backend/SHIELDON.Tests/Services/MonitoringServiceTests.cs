using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class MonitoringServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public MonitoringServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetTutorDashboardAsync_WithValidTutor_ShouldReturnDashboard()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var tutorId = Guid.NewGuid();
        var service = new MonitoringService(dbContext);

        // Act
        var dashboard = await service.GetTutorDashboardAsync(tutorId);

        // Assert
        dashboard.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithInvalidAttempt_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new MonitoringService(dbContext);

        // Act
        Func<Task> act = async () => await service.ProcessHeartbeatAsync(Guid.NewGuid(), Guid.NewGuid(), false);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
