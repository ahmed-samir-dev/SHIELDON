using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Security.Leaderboard;

/// <summary>
/// Security tests for the Leaderboard module.
/// Validates: hidden leaderboard access control, non-existent course protection.
/// </summary>
public class LeaderboardSecurityTests
{
    private AppDbContext CreateDbContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // ─── Hidden Leaderboard: Student Access Must Be Filtered ─────────────────

    [Fact]
    public async Task GetLeaderboard_WithLeaderboardDisabled_ShouldThrowOrReturnEmpty()
    {
        // Arrange
        using var db = CreateDbContext();
        var adminId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        db.Courses.Add(new Course
        {
            Id = courseId, CourseCode = "HIDDEN101", Title = "Hidden LB Course",
            CreatedByAdminId = adminId, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.LeaderboardSettings.Add(new LeaderboardSettings
        {
            Id = Guid.NewGuid(), CourseId = courseId,
            IsLeaderboardVisible = false // leaderboard is hidden for students
        });
        await db.SaveChangesAsync();

        var service = new LeaderboardService(db);

        // Act
        var response = await service.GetLeaderboardAsync(courseId, studentId, "Student");

        // Assert - disabled leaderboard returns empty top entries and IsLeaderboardVisible = false
        response.Should().NotBeNull();
        response.IsLeaderboardVisible.Should().BeFalse("disabled leaderboard must have IsLeaderboardVisible = false");
        response.TopEntries.Should().BeEmpty("students must receive an empty top entries list when leaderboard is disabled");
    }

    // ─── Non-Existent Course Returns 404 ──────────────────────────────────────

    [Fact]
    public async Task GetLeaderboard_WithNonExistentCourse_ShouldThrowNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = new LeaderboardService(db);

        // Act
        Func<Task> act = () => service.GetLeaderboardAsync(Guid.NewGuid(), Guid.NewGuid(), "Student");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>(
            "requesting leaderboard for a non-existent course must return 404");
    }

    // ─── Admin Can Always Access Leaderboard ──────────────────────────────────

    [Fact]
    public async Task GetLeaderboard_ByAdmin_ShouldSucceed()
    {
        // Arrange
        using var db = CreateDbContext();
        var adminId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        db.Courses.Add(new Course
        {
            Id = courseId, CourseCode = "ADMIN101", Title = "Admin Course",
            CreatedByAdminId = adminId, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new LeaderboardService(db);

        // Act
        var result = await service.GetLeaderboardAsync(courseId, adminId, "Admin");

        // Assert
        result.Should().NotBeNull("admins must always be able to access any leaderboard");
    }

    // ─── Tutor Can Access Their Own Course Leaderboard ────────────────────────

    [Fact]
    public async Task GetLeaderboard_ByTutor_ShouldSucceed()
    {
        // Arrange
        using var db = CreateDbContext();
        var tutorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        db.Courses.Add(new Course
        {
            Id = courseId, CourseCode = "TUT101", Title = "Tutor Course",
            AssignedTutorId = tutorId, CreatedByAdminId = adminId, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new LeaderboardService(db);

        // Act
        var result = await service.GetLeaderboardAsync(courseId, tutorId, "Tutor");

        // Assert
        result.Should().NotBeNull("tutors must be able to access the leaderboard for their assigned courses");
    }
}
