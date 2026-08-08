using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Violations.DTOs;
using SHIELDON.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class ViolationServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public ViolationServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task LogViolationBatchAsync_WithEmptyList_ShouldReturnOk()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ViolationService(dbContext, Mock.Of<IDashboardNotificationService>());
        var request = new BatchViolationRequest(new List<ViolationLogRequest>());

        // Act
        var result = await service.LogViolationBatchAsync(request, Guid.NewGuid());

        // Assert
        result.Message.Should().Be("No violations to log.");
    }

    [Fact]
    public async Task LogViolationBatchAsync_WithInvalidAttempt_ShouldSkipLog()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ViolationService(dbContext, Mock.Of<IDashboardNotificationService>());
        var logs = new List<ViolationLogRequest> { new ViolationLogRequest(Guid.NewGuid(), ViolationType.TabSwitch, ViolationSeverity.Minor, "Switched tab", DateTime.UtcNow, false) };
        var request = new BatchViolationRequest(logs);

        // Act
        var result = await service.LogViolationBatchAsync(request, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
    }
}
