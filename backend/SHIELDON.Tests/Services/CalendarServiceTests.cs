using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class CalendarServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public CalendarServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetCalendarEventsAsync_WithNonExistentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new CalendarService(dbContext, Mock.Of<ILogger<CalendarService>>());

        // Act
        Func<Task> act = async () => await service.GetCalendarEventsAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(7), default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
