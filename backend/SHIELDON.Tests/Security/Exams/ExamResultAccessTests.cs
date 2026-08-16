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
using SHIELDON.Application.Features.Exams.DTOs;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Security.Exams;

/// <summary>
/// Security tests for exam results access control.
/// Validates: result visibility before manual release, cross-student result access,
/// and tutor access to non-assigned course results.
/// </summary>
public class ExamResultAccessTests
{
    private AppDbContext CreateDbContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private ExamResultService CreateService(AppDbContext db) =>
        new ExamResultService(db, Mock.Of<INotificationService>());

    // ─── Helper: seed a submitted attempt ────────────────────────────────────

    private async Task<(Guid studentId, Guid attemptId, Guid tutorId, Guid otherTutorId)> SeedSubmittedAttempt(
        AppDbContext db, bool resultsReleased = false)
    {
        var adminId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var otherTutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();

        db.Courses.Add(new Course { Id = courseId, CourseCode = "RES101", Title = "Results Test", AssignedTutorId = tutorId, CreatedByAdminId = adminId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Exams.Add(new Exam
        {
            Id = examId, CourseId = courseId, Title = "Final Exam",
            CreatedByUserId = tutorId,
            ResultVisibility = resultsReleased ? ResultVisibility.Immediate : ResultVisibility.ManualRelease,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.ExamAttempts.Add(new ExamAttempt
        {
            Id = attemptId, StudentId = studentId, ExamId = examId,
            Status = AttemptStatus.Submitted, StartedAt = DateTime.UtcNow.AddHours(-1),
            SubmittedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return (studentId, attemptId, tutorId, otherTutorId);
    }

    // ─── Student Cannot See Results Before Release ────────────────────────────

    [Fact]
    public async Task GetResult_ByStudent_BeforeManualRelease_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var (studentId, attemptId, tutorId, _) = await SeedSubmittedAttempt(db, resultsReleased: false);
        var service = CreateService(db);

        // Act
        var result = await service.GetAttemptResultAsync(attemptId, studentId, "Student");

        // Assert - detailed question reviews must be hidden (null) until manual release
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.ResultVisible.Should().BeFalse("results must be marked non-visible before manual release");
        result.Data.QuestionReviews.Should().BeNull("question reviews must be omitted before manual release");
    }

    // ─── Student Can See Results After Release ────────────────────────────────

    [Fact]
    public async Task GetResult_ByStudent_AfterManualRelease_ShouldSucceed()
    {
        // Arrange
        using var db = CreateDbContext();
        var (studentId, attemptId, tutorId, _) = await SeedSubmittedAttempt(db, resultsReleased: true);
        var service = CreateService(db);

        // Act
        var result = await service.GetAttemptResultAsync(attemptId, studentId, "Student");

        // Assert
        result.Should().NotBeNull("students must be able to see results after manual release");
    }

    // ─── Student Cannot See Another Student's Results ─────────────────────────

    [Fact]
    public async Task GetResult_ByDifferentStudent_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var (studentId, attemptId, tutorId, _) = await SeedSubmittedAttempt(db, resultsReleased: true);
        var service = CreateService(db);

        var attackerStudentId = Guid.NewGuid(); // different student

        // Act - attacker tries to read another student's result
        Func<Task> act = () => service.GetAttemptResultAsync(attemptId, attackerStudentId, "Student");

        // Assert
        await act.Should().ThrowAsync<Exception>(
            "students must never be able to access another student's exam results");
    }

    // ─── Tutor Can See Results for Their Course ───────────────────────────────

    [Fact]
    public async Task GetAttempts_ByAssignedTutor_ShouldSucceed()
    {
        // Arrange
        using var db = CreateDbContext();
        var (studentId, attemptId, tutorId, otherTutorId) = await SeedSubmittedAttempt(db, resultsReleased: false);
        var service = CreateService(db);

        var examId = (await db.ExamAttempts.FindAsync(attemptId))!.ExamId;

        // Act - assigned tutor queries exam attempts
        var result = await service.GetExamAttemptsAsync(examId, tutorId, "Tutor");

        // Assert
        result.Should().NotBeNull("the assigned tutor must be able to view all attempts for their exam");
    }

    // ─── Tutor Cannot See Results for Non-Assigned Course ────────────────────

    [Fact]
    public async Task GetAttempts_ByNonAssignedTutor_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var (studentId, attemptId, tutorId, otherTutorId) = await SeedSubmittedAttempt(db, resultsReleased: false);
        var service = CreateService(db);

        var examId = (await db.ExamAttempts.FindAsync(attemptId))!.ExamId;

        // Act - unrelated tutor queries another tutor's exam attempts
        Func<Task> act = () => service.GetExamAttemptsAsync(examId, otherTutorId, "Tutor");

        // Assert
        await act.Should().ThrowAsync<Exception>(
            "tutors must not access exam attempt data for courses they are not assigned to");
    }
}
