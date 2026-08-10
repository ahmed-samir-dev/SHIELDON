using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Exams.DTOs;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Services;

public class ExamAttemptServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public ExamAttemptServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task SaveAnswer_ForNonExistentAttempt_ShouldThrowNotFoundException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var service = new ExamAttemptService(dbContext, Mock.Of<INotificationService>(), Mock.Of<IDashboardNotificationService>());
        var request = new SaveAnswerRequest { QuestionId = Guid.NewGuid(), SelectedOptionId = Guid.NewGuid(), TextAnswer = null, IsFlagged = false };

        // Act
        Func<Task> act = async () => await service.SaveAnswerAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SaveAnswer_WhenAttemptSubmitted_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var dbContext = new AppDbContext(_dbOptions);
        var attemptId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var tokenGuid = Guid.NewGuid();

        var attempt = new ExamAttempt
        {
            Id = attemptId,
            StudentId = studentId,
            ExamId = Guid.NewGuid(),
            Status = AttemptStatus.Submitted, // Already submitted
            StartedAt = DateTime.UtcNow
        };
        var token = new ExamToken
        {
            AttemptId = attemptId,
            Token = tokenGuid,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
        attempt.Token = token;

        dbContext.ExamAttempts.Add(attempt);
        await dbContext.SaveChangesAsync();

        var service = new ExamAttemptService(dbContext, Mock.Of<INotificationService>(), Mock.Of<IDashboardNotificationService>());
        var request = new SaveAnswerRequest { QuestionId = Guid.NewGuid(), SelectedOptionId = Guid.NewGuid(), TextAnswer = null, IsFlagged = false };

        // Act
        Func<Task> act = async () => await service.SaveAnswerAsync(attemptId, tokenGuid, request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Attempt has already been submitted.");
    }
}
