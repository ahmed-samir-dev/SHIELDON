using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class ExamResultServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public ExamResultServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetAttemptResultAsync_WithInvalidAttempt_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ExamResultService(dbContext, Mock.Of<INotificationService>());

        // Act
        Func<Task> act = async () => await service.GetAttemptResultAsync(Guid.NewGuid(), Guid.NewGuid(), "Student");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetExamAttemptsAsync_WithInvalidExam_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ExamResultService(dbContext, Mock.Of<INotificationService>());

        // Act
        Func<Task> act = async () => await service.GetExamAttemptsAsync(Guid.NewGuid(), Guid.NewGuid(), "Tutor");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
