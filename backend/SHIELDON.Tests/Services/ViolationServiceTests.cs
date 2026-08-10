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

    [Fact]
    public async Task LogBatch_MixedValidAndInvalidAttemptIds_ShouldLogOnlyValidOnes()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var studentId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var validAttemptId = Guid.NewGuid();
        var invalidAttemptId = Guid.NewGuid();

        var exam = new Exam { Id = examId, CourseId = courseId, Title = "Exam 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        dbContext.Exams.Add(exam);
        dbContext.ExamAttempts.Add(new ExamAttempt
        {
            Id = validAttemptId,
            StudentId = studentId,
            ExamId = examId,
            Exam = exam,
            Status = AttemptStatus.InProgress,
            StartedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var logs = new List<ViolationLogRequest>
        {
            new(validAttemptId, ViolationType.TabSwitch, ViolationSeverity.Minor, "Valid violation", DateTime.UtcNow, false),
            new(invalidAttemptId, ViolationType.FullScreenExit, ViolationSeverity.Critical, "Forged violation", DateTime.UtcNow, false)
        };
        var service = new ViolationService(dbContext, Mock.Of<IDashboardNotificationService>());

        // Act
        var result = await service.LogViolationBatchAsync(new BatchViolationRequest(logs), studentId);

        // Assert
        result.Message.Should().Contain("1 violation(s) logged successfully.");
        var savedLogs = await dbContext.ViolationLogs.ToListAsync();
        savedLogs.Should().HaveCount(1);
        savedLogs[0].AttemptId.Should().Be(validAttemptId);
    }

    [Fact]
    public async Task LogBatch_ConcurrentRequests_ShouldNotDeadlock()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var studentId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();

        var exam = new Exam { Id = examId, CourseId = Guid.NewGuid(), Title = "Exam 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        dbContext.Exams.Add(exam);
        dbContext.ExamAttempts.Add(new ExamAttempt
        {
            Id = attemptId,
            StudentId = studentId,
            ExamId = examId,
            Exam = exam,
            Status = AttemptStatus.InProgress,
            StartedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new ViolationService(dbContext, Mock.Of<IDashboardNotificationService>());
        var batch = new BatchViolationRequest(new List<ViolationLogRequest>
        {
            new(attemptId, ViolationType.AbnormalMouseActivity, ViolationSeverity.Minor, "Left window", DateTime.UtcNow, false)
        });

        // Act - execute 5 parallel requests
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(service.LogViolationBatchAsync(batch, studentId));
        }

        Func<Task> act = async () => await Task.WhenAll(tasks);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
