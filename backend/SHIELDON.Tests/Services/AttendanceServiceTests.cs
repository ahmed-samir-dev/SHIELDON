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

public class AttendanceServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public AttendanceServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task StartCheckAsync_WithValidCourse_ShouldCreateCheck()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new AttendanceService(dbContext);

        // Act
        var result = await service.StartCheckAsync(Guid.NewGuid(), Guid.NewGuid(), "Lecture 1");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Lecture 1");
    }

    [Fact]
    public async Task EndCheckAsync_WithNonExistentCheck_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new AttendanceService(dbContext);

        // Act
        Func<Task> act = async () => await service.EndCheckAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
