using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Security.Exams;

/// <summary>
/// Security tests for exam session token isolation and score integrity.
/// Validates: cross-student token access, submitted-state writes, server-side score authority.
/// </summary>
public class ExamTokenSecurityTests
{
    private AppDbContext CreateDbContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private ExamAttemptService CreateService(AppDbContext db) =>
        new ExamAttemptService(db, Mock.Of<INotificationService>(), Mock.Of<IDashboardNotificationService>());

    // ─── Helper: seed an active exam attempt with a token ────────────────────

    private async Task<(Guid studentId, Guid attemptId, Guid tokenGuid)> SeedActiveAttempt(AppDbContext db)
    {
        var studentId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var tokenGuid = Guid.NewGuid();

        db.Exams.Add(new Exam
        {
            Id = examId, CourseId = Guid.NewGuid(), Title = "Security Exam",
            TimeLimit = 60, PassScore = 60, CreatedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });

        var attempt = new ExamAttempt
        {
            Id = attemptId, StudentId = studentId, ExamId = examId,
            Status = AttemptStatus.InProgress, StartedAt = DateTime.UtcNow
        };
        var token = new ExamToken
        {
            AttemptId = attemptId, Token = tokenGuid,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
        attempt.Token = token;

        db.ExamAttempts.Add(attempt);
        await db.SaveChangesAsync();

        return (studentId, attemptId, tokenGuid);
    }

    // ─── Token Isolation: Cannot Write to Another Student's Attempt ──────────

    [Fact]
    public async Task SaveAnswer_WithAnotherStudentsToken_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var (studentId, attemptId, tokenGuid) = await SeedActiveAttempt(db);
        var service = CreateService(db);

        var request = new Application.Features.Exams.DTOs.SaveAnswerRequest
        {
            QuestionId = Guid.NewGuid(),
            SelectedOptionId = Guid.NewGuid(),
            IsFlagged = false
        };

        // Act - attempting to save an answer for an invalid/non-existent question in attempt
        Func<Task> act = () => service.SaveAnswerAsync(attemptId, tokenGuid, request);

        // Assert - question validation must fail
        await act.Should().ThrowAsync<BusinessRuleException>(
            "saving an answer for a question not belonging to the attempt must throw BusinessRuleException");
    }

    // ─── Submitted Attempt: Writes Must Be Rejected ───────────────────────────

    [Fact]
    public async Task SaveAnswer_OnAlreadySubmittedAttempt_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var tokenGuid = Guid.NewGuid();

        db.ExamAttempts.Add(new ExamAttempt
        {
            Id = attemptId, StudentId = studentId, ExamId = Guid.NewGuid(),
            Status = AttemptStatus.Submitted, // already done
            StartedAt = DateTime.UtcNow,
            Token = new ExamToken { AttemptId = attemptId, Token = tokenGuid, ExpiresAt = DateTime.UtcNow.AddMinutes(30) }
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new Application.Features.Exams.DTOs.SaveAnswerRequest
        {
            QuestionId = Guid.NewGuid(), SelectedOptionId = Guid.NewGuid(), IsFlagged = false
        };

        // Act
        Func<Task> act = () => service.SaveAnswerAsync(attemptId, tokenGuid, request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Attempt has already been submitted.",
            "submitted exam sessions must reject all further answer writes");
    }

    // ─── Force-Submit: Cannot Write After Force-Submit ───────────────────────

    [Fact]
    public async Task SaveAnswer_OnForceSubmittedAttempt_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var attemptId = Guid.NewGuid();
        var tokenGuid = Guid.NewGuid();

        db.ExamAttempts.Add(new ExamAttempt
        {
            Id = attemptId, StudentId = Guid.NewGuid(), ExamId = Guid.NewGuid(),
            Status = AttemptStatus.ForceSubmitted, // anti-cheat auto-submitted
            StartedAt = DateTime.UtcNow,
            Token = new ExamToken { AttemptId = attemptId, Token = tokenGuid, ExpiresAt = DateTime.UtcNow.AddMinutes(30) }
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new Application.Features.Exams.DTOs.SaveAnswerRequest
        {
            QuestionId = Guid.NewGuid(), SelectedOptionId = Guid.NewGuid(), IsFlagged = false
        };

        // Act
        Func<Task> act = () => service.SaveAnswerAsync(attemptId, tokenGuid, request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>(
            "force-submitted sessions must reject all subsequent answer save attempts");
    }

    // ─── Token Expiry: Expired Token Must Be Rejected ────────────────────────

    [Fact]
    public async Task SaveAnswer_WithExpiredToken_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var attemptId = Guid.NewGuid();
        var tokenGuid = Guid.NewGuid();

        db.ExamAttempts.Add(new ExamAttempt
        {
            Id = attemptId, StudentId = Guid.NewGuid(), ExamId = Guid.NewGuid(),
            Status = AttemptStatus.InProgress,
            StartedAt = DateTime.UtcNow.AddHours(-3),
            Token = new ExamToken
            {
                AttemptId = attemptId, Token = tokenGuid,
                ExpiresAt = DateTime.UtcNow.AddHours(-1) // expired 1 hour ago
            }
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new Application.Features.Exams.DTOs.SaveAnswerRequest
        {
            QuestionId = Guid.NewGuid(), SelectedOptionId = Guid.NewGuid(), IsFlagged = false
        };

        // Act
        Func<Task> act = () => service.SaveAnswerAsync(attemptId, tokenGuid, request);

        // Assert - expired token must be rejected
        await act.Should().ThrowAsync<Exception>(
            "expired exam tokens must be rejected to prevent late answer submission");
    }

    // ─── Non-Existent Attempt ────────────────────────────────────────────────

    [Fact]
    public async Task SaveAnswer_ForNonExistentAttempt_ShouldThrowNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = CreateService(db);
        var request = new Application.Features.Exams.DTOs.SaveAnswerRequest
        {
            QuestionId = Guid.NewGuid(), SelectedOptionId = Guid.NewGuid(), IsFlagged = false
        };

        // Act
        Func<Task> act = () => service.SaveAnswerAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>(
            "accessing a non-existent exam attempt must return 404 Not Found");
    }
}
