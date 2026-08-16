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

using Microsoft.Extensions.Caching.Memory;

namespace SHIELDON.Tests.Security.Monitoring;

/// <summary>
/// Security-focused RBAC tests for the Monitoring subsystem.
/// Validates: session timeline access control across Student/Tutor/Admin roles.
/// </summary>
public class MonitoringRBACTests
{
    private AppDbContext CreateDbContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private MonitoringService CreateService(AppDbContext db) => new MonitoringService(db, new MemoryCache(new MemoryCacheOptions()));

    // ─── Helper: seed a tutor with courses and an active exam session ─────────

    private async Task<(AppDbContext db, Guid adminId, Guid tutorId, Guid otherTutorId, Guid studentId, Guid attemptId)> SeedMonitoringData()
    {
        var db = CreateDbContext();
        var adminId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var otherTutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = adminId, Email = "admin@test.com", FirstName = "A", LastName = "D", Role = UserRole.Admin, PasswordHash = "h", AccountStatus = AccountStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = tutorId, Email = "tutor@test.com", FirstName = "T", LastName = "U", Role = UserRole.Tutor, PasswordHash = "h", AccountStatus = AccountStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = otherTutorId, Email = "other@test.com", FirstName = "O", LastName = "T", Role = UserRole.Tutor, PasswordHash = "h", AccountStatus = AccountStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new User { Id = studentId, Email = "student@test.com", FirstName = "S", LastName = "T", Role = UserRole.Student, PasswordHash = "h", AccountStatus = AccountStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        db.Courses.Add(new Course { Id = courseId, Title = "Sec Course", CourseCode = "SEC1", AssignedTutorId = tutorId, CreatedByAdminId = adminId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.CourseEnrollments.Add(new CourseEnrollment { Id = Guid.NewGuid(), CourseId = courseId, StudentId = studentId, Status = CourseEnrollmentStatus.Approved, RequestedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Exams.Add(new Exam { Id = examId, CourseId = courseId, Title = "Midterm", CreatedByUserId = tutorId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.ExamAttempts.Add(new ExamAttempt { Id = attemptId, StudentId = studentId, ExamId = examId, Status = AttemptStatus.InProgress, StartedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return (db, adminId, tutorId, otherTutorId, studentId, attemptId);
    }

    // ─── Admin: System-Wide Access Allowed ───────────────────────────────────

    [Fact]
    public async Task GetTutorDashboard_ByAdmin_ShouldSucceed()
    {
        // Arrange
        var (db, adminId, tutorId, _, _, _) = await SeedMonitoringData();
        var service = CreateService(db);

        // Act
        var result = await service.GetTutorDashboardAsync(adminId);

        // Assert
        result.Should().NotBeNull("admins must always be able to access dashboard data");

        db.Dispose();
    }

    // ─── Tutor: Own Dashboard Only ────────────────────────────────────────────

    [Fact]
    public async Task GetTutorDashboard_ByAssignedTutor_ShouldSucceed()
    {
        // Arrange
        var (db, adminId, tutorId, _, _, _) = await SeedMonitoringData();
        var service = CreateService(db);

        // Act
        var result = await service.GetTutorDashboardAsync(tutorId);

        // Assert
        result.Should().NotBeNull("assigned tutors must be able to access their own dashboard");

        db.Dispose();
    }

    [Fact]
    public async Task GetTutorDashboard_ByOtherTutor_ShouldReturnEmptyData()
    {
        // Arrange
        var (db, adminId, tutorId, otherTutorId, _, _) = await SeedMonitoringData();
        var service = CreateService(db);

        // Act - other tutor (no courses assigned) gets their own (empty) dashboard
        var result = await service.GetTutorDashboardAsync(otherTutorId);

        // Assert - should return empty data for the unrelated tutor, not another's data
        result.Should().NotBeNull();
        result.ExamSummaries.Should().BeEmpty(
            "a tutor with no assigned courses must see 0 exam summaries - not another tutor's data");

        db.Dispose();
    }

    // ─── Heartbeat: Non-Existent Attempt Returns 404 ─────────────────────────

    [Fact]
    public async Task ProcessHeartbeat_ForNonExistentAttempt_ShouldThrowNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = CreateService(db);

        // Act
        Func<Task> act = () => service.ProcessHeartbeatAsync(Guid.NewGuid(), Guid.NewGuid(), false);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>(
            "heartbeats for non-existent attempt IDs must return 404");
    }

    // ─── Heartbeat: Correct Student Must Own the Attempt ─────────────────────

    [Fact]
    public async Task ProcessHeartbeat_WithWrongStudentId_ShouldThrowOrReturnNotFound()
    {
        // Arrange
        var (db, _, _, _, studentId, attemptId) = await SeedMonitoringData();
        var service = CreateService(db);

        var attackerStudentId = Guid.NewGuid(); // not the real student

        // Act - attacker sends heartbeat claiming ownership of another student's attempt
        Func<Task> act = () => service.ProcessHeartbeatAsync(attemptId, attackerStudentId, false);

        // Assert - must be rejected
        await act.Should().ThrowAsync<Exception>(
            "heartbeats from a student that does not own the attempt must be rejected");

        db.Dispose();
    }
}
