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

public class LeaderboardServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public LeaderboardServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetLeaderboardAsync_WithInvalidCourse_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new LeaderboardService(dbContext);

        // Act
        Func<Task> act = async () => await service.GetLeaderboardAsync(Guid.NewGuid(), Guid.NewGuid(), "Student");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetLeaderboardAsync_WithValidCourse_ShouldReturnLeaderboard()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var courseId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        dbContext.Courses.Add(new Course { Id = courseId, CourseCode = "CS501", Title = "AI", CreatedByAdminId = adminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        var service = new LeaderboardService(dbContext);

        // Act
        var leaderboard = await service.GetLeaderboardAsync(courseId, adminId, "Admin");

        // Assert
        leaderboard.Should().NotBeNull();
    }
}
