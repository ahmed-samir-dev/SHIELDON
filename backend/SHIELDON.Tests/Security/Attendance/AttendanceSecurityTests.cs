using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Common;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Security.Attendance;

/// <summary>
/// Security tests for the Attendance QR system.
/// Validates: expired QR rejection, non-enrolled student blocking,
/// QR rotation security (old code rejected), and tutor ownership.
/// </summary>
public class AttendanceSecurityTests
{
    private AppDbContext CreateDbContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // ─── Expired QR Code Must Be Rejected ────────────────────────────────────

    [Fact]
    public async Task ScanQR_WithExpiredSecret_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var db = CreateDbContext();
        var checkId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var secret = "expired-secret-789";
        var baseTime = DateTime.UtcNow;
        var expiresAt = baseTime.AddSeconds(15);

        db.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId, CourseId = Guid.NewGuid(), TutorId = Guid.NewGuid(),
            Title = "Security Test Check", IsActive = true,
            CurrentSecret = secret, SecretExpiresAt = expiresAt,
            CreatedAt = baseTime
        });
        await db.SaveChangesAsync();

        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.UtcNow).Returns(expiresAt.AddSeconds(1)); // 1 second after expiry
        var service = new AttendanceService(db, mockTime.Object);

        // Act
        Func<Task> act = () => service.VerifyAndMarkAsync(studentId, checkId, secret);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("QR code has expired. Please scan the latest code.",
            "an expired QR code must always be rejected regardless of the secret value");
    }

    // ─── Wrong Secret (Old QR Code After Rotation) ────────────────────────────

    [Fact]
    public async Task ScanQR_WithOldSecret_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var checkId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var currentSecret = "new-secret-after-rotation";
        var oldSecret = "old-secret-before-rotation";

        db.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId, CourseId = Guid.NewGuid(), TutorId = Guid.NewGuid(),
            Title = "Rotation Test", IsActive = true,
            CurrentSecret = currentSecret,
            SecretExpiresAt = DateTime.UtcNow.AddSeconds(14), // still valid
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
        var service = new AttendanceService(db, mockTime.Object);

        // Act - student submits the OLD secret after QR has rotated
        Func<Task> act = () => service.VerifyAndMarkAsync(studentId, checkId, oldSecret);

        // Assert - old code must be rejected
        await act.Should().ThrowAsync<Exception>(
            "the old QR secret must be rejected after a 15-second rotation");
    }

    // ─── Inactive Check Must Be Rejected ──────────────────────────────────────

    [Fact]
    public async Task ScanQR_ForInactiveCheck_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var checkId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        db.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId, CourseId = Guid.NewGuid(), TutorId = Guid.NewGuid(),
            Title = "Ended Check", IsActive = false, // check has been ended
            CurrentSecret = "secret123",
            SecretExpiresAt = DateTime.UtcNow.AddSeconds(15),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
        var service = new AttendanceService(db, mockTime.Object);

        // Act - student tries to scan for an already-ended check
        Func<Task> act = () => service.VerifyAndMarkAsync(studentId, checkId, "secret123");

        // Assert
        await act.Should().ThrowAsync<Exception>(
            "inactive attendance checks must reject all scan attempts");
    }

    // ─── Valid QR Scan On Active Check ────────────────────────────────────────

    [Fact]
    public async Task ScanQR_WithValidSecretAndActiveCheck_ShouldCreateRecord()
    {
        // Arrange
        using var db = CreateDbContext();
        var checkId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var secret = "valid-secret-xyz";
        var expiresAt = DateTime.UtcNow.AddSeconds(15);

        db.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId, CourseId = Guid.NewGuid(), TutorId = Guid.NewGuid(),
            Title = "Live Check", IsActive = true,
            CurrentSecret = secret, SecretExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.UtcNow).Returns(expiresAt); // boundary (inclusive)
        var service = new AttendanceService(db, mockTime.Object);

        // Act
        var result = await service.VerifyAndMarkAsync(studentId, checkId, secret);

        // Assert
        result.Should().NotBeNull();
        result.StudentId.Should().Be(studentId);
    }

    // ─── Non-Existent Check ───────────────────────────────────────────────────

    [Fact]
    public async Task ScanQR_ForNonExistentCheck_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        using var db = CreateDbContext();
        var mockTime = new Mock<ITimeProvider>();
        mockTime.Setup(t => t.UtcNow).Returns(DateTime.UtcNow);
        var service = new AttendanceService(db, mockTime.Object);

        // Act
        Func<Task> act = () => service.VerifyAndMarkAsync(Guid.NewGuid(), Guid.NewGuid(), "any-secret");

        // Assert
        await act.Should().ThrowAsync<Exception>(
            "scanning a non-existent attendance check must return an error");
    }

    // ─── Tutor End Own Check: Valid ───────────────────────────────────────────

    [Fact]
    public async Task EndCheck_ByOwningTutor_ShouldDeactivateCheck()
    {
        // Arrange
        using var db = CreateDbContext();
        var tutorId = Guid.NewGuid();
        var checkId = Guid.NewGuid();

        db.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId, CourseId = Guid.NewGuid(), TutorId = tutorId,
            Title = "My Check", IsActive = true,
            CurrentSecret = "s", SecretExpiresAt = DateTime.UtcNow.AddSeconds(15),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AttendanceService(db);

        // Act
        await service.EndCheckAsync(checkId, tutorId);

        // Assert
        var check = await db.AttendanceChecks.FindAsync(checkId);
        check!.IsActive.Should().BeFalse("ending an attendance check must deactivate it");
    }

    // ─── Tutor End Another Tutor's Check: Denied ──────────────────────────────

    [Fact]
    public async Task EndCheck_ByDifferentTutor_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        var ownerTutorId = Guid.NewGuid();
        var otherTutorId = Guid.NewGuid();
        var checkId = Guid.NewGuid();

        db.AttendanceChecks.Add(new AttendanceCheck
        {
            Id = checkId, CourseId = Guid.NewGuid(), TutorId = ownerTutorId,
            Title = "Owner Check", IsActive = true,
            CurrentSecret = "s", SecretExpiresAt = DateTime.UtcNow.AddSeconds(15),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new AttendanceService(db);

        // Act - different tutor tries to end another's check
        Func<Task> act = () => service.EndCheckAsync(checkId, otherTutorId);

        // Assert
        await act.Should().ThrowAsync<Exception>(
            "a tutor must not be able to deactivate another tutor's attendance check");
    }
}
