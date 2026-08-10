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

    [Fact]
    public async Task QrCode_ScannedAtExactExpirationMillisecond_ShouldSucceed()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var checkId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var secret = "secret123";
        var baseTime = DateTime.UtcNow;
        var expiresAt = baseTime.AddSeconds(15);

        dbContext.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId,
            CourseId = Guid.NewGuid(),
            TutorId = Guid.NewGuid(),
            Title = "Test Check",
            IsActive = true,
            CurrentSecret = secret,
            SecretExpiresAt = expiresAt,
            CreatedAt = baseTime
        });
        await dbContext.SaveChangesAsync();

        var mockTime = new Mock<SHIELDON.Application.Common.ITimeProvider>();
        mockTime.Setup(t => t.UtcNow).Returns(expiresAt); // Exact millisecond boundary

        var service = new AttendanceService(dbContext, mockTime.Object);

        // Act
        var result = await service.VerifyAndMarkAsync(studentId, checkId, secret);

        // Assert
        result.Should().NotBeNull();
        result.StudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task QrCode_ScannedOneMillisecondAfterExpiration_ShouldFail()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var checkId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var secret = "secret123";
        var baseTime = DateTime.UtcNow;
        var expiresAt = baseTime.AddSeconds(15);

        dbContext.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId,
            CourseId = Guid.NewGuid(),
            TutorId = Guid.NewGuid(),
            Title = "Test Check",
            IsActive = true,
            CurrentSecret = secret,
            SecretExpiresAt = expiresAt,
            CreatedAt = baseTime
        });
        await dbContext.SaveChangesAsync();

        var mockTime = new Mock<SHIELDON.Application.Common.ITimeProvider>();
        mockTime.Setup(t => t.UtcNow).Returns(expiresAt.AddMilliseconds(1)); // 1ms after boundary

        var service = new AttendanceService(dbContext, mockTime.Object);

        // Act
        Func<Task> act = async () => await service.VerifyAndMarkAsync(studentId, checkId, secret);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("QR code has expired. Please scan the latest code.");
    }
}
