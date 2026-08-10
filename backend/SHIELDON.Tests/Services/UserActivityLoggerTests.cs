using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SHIELDON.Domain.Entities;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using Xunit;

namespace SHIELDON.Tests.Services;

public class UserActivityLoggerTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public UserActivityLoggerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task LogAsync_ShouldPersistExpandedUserActivityLog()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var logger = new NullLogger<UserActivityLogger>();

        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppDbContext))).Returns(dbContext);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);

        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);

        var activityLogger = new UserActivityLogger(mockScopeFactory.Object, mockHttpContextAccessor.Object, logger);
        var userId = Guid.NewGuid();

        // Act
        await activityLogger.LogAsync(
            userId,
            "EXAM",
            "ExamAttemptStarted",
            "Student started exam attempt",
            entityId: "attempt-123",
            entityType: "ExamAttempt",
            metadata: new { QuestionCount = 10 });

        // Assert
        var savedLog = await dbContext.UserActivityLogs.FirstOrDefaultAsync();
        savedLog.Should().NotBeNull();
        savedLog!.UserId.Should().Be(userId);
        savedLog.Category.Should().Be("EXAM");
        savedLog.Action.Should().Be("ExamAttemptStarted");
        savedLog.Description.Should().Be("Student started exam attempt");
        savedLog.EntityId.Should().Be("attempt-123");
        savedLog.EntityType.Should().Be("ExamAttempt");
        savedLog.MetadataJson.Should().Contain("QuestionCount");
    }

    [Fact]
    public async Task NullUserActivityLogger_ShouldExecuteWithoutError()
    {
        // Arrange
        var nullLogger = new SHIELDON.Application.Common.NullUserActivityLogger();

        // Act
        var act = () => nullLogger.LogAsync(Guid.NewGuid(), "AUTH", "UserLogin", "Logged in");

        // Assert
        await act.Should().NotThrowAsync();
    }
}
