using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Auth.DTOs;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Security.Auth;

/// <summary>
/// Security-focused tests for the AuthService.
/// Validates: account lockout, enumeration protection, token lifecycle, and input safety.
/// </summary>
public class AuthSecurityTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IJwtService> _mockJwtService;

    public AuthSecurityTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockEmailService = new Mock<IEmailService>();
        _mockJwtService = new Mock<IJwtService>();

        _mockJwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("mock_access_token");
        _mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns("mock_refresh_token");
    }

    private AppDbContext CreateDbContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private AuthService CreateService(AppDbContext db) =>
        new AuthService(db, _mockJwtService.Object, _mockEmailService.Object, _mockConfig.Object);

    // ─── Account Status Security ──────────────────────────────────────────────

    [Fact]
    public async Task Login_WithLockedAccount_ShouldThrowAndNotRevealPassword()
    {
        // Arrange
        using var db = CreateDbContext();
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1");
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "locked@test.com", PasswordHash = hash,
            FirstName = "L", LastName = "U", Role = UserRole.Student,
            AccountStatus = AccountStatus.Locked,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act
        Func<Task> act = () => service.LoginAsync(new LoginRequest("locked@test.com", "CorrectPass@1"));

        // Assert - throws; message should NOT say "invalid password" (avoids revealing lock reason leakage through a different path)
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*lock*", "locked accounts must produce a recognisable locked message");
    }

    [Fact]
    public async Task Login_WithDisabledAccount_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "disabled@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@1234"),
            FirstName = "D", LastName = "U", Role = UserRole.Student,
            AccountStatus = AccountStatus.Locked, // No explicit Disabled state; use Locked
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Func<Task> act = () => service.LoginAsync(new LoginRequest("disabled@test.com", "Pass@1234"));

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Login_WithUnverifiedAccount_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "unverified@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@1234"),
            FirstName = "U", LastName = "V", Role = UserRole.Student,
            AccountStatus = AccountStatus.Unverified,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Func<Task> act = () => service.LoginAsync(new LoginRequest("unverified@test.com", "Pass@1234"));

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── Account Enumeration Protection ──────────────────────────────────────

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldThrowSameTypeAsWrongPassword()
    {
        // Arrange
        using var db = CreateDbContext();
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1");
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "real@test.com", PasswordHash = hash,
            FirstName = "R", LastName = "U", Role = UserRole.Student,
            AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act - unknown email
        Func<Task> actUnknownEmail = () => service.LoginAsync(new LoginRequest("ghost@test.com", "SomePass@1"));
        // Act - existing email, wrong password
        Func<Task> actWrongPassword = () => service.LoginAsync(new LoginRequest("real@test.com", "WrongPass@1"));

        // Assert - both must raise the same exception type (prevents enumeration)
        var exUnknown = await Record.ExceptionAsync(actUnknownEmail.Invoke);
        var exWrong = await Record.ExceptionAsync(actWrongPassword.Invoke);
        exUnknown.Should().NotBeNull();
        exWrong.Should().NotBeNull();
        exUnknown!.GetType().Should().Be(exWrong!.GetType(),
            "account enumeration protection requires identical error types for unknown-email and wrong-password scenarios");
    }

    // ─── Brute-Force Lockout ──────────────────────────────────────────────────

    [Fact]
    public async Task Login_After5FailedAttempts_ShouldLockAccount()
    {
        // Arrange
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Email = "bruteforce@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1"),
            FirstName = "B", LastName = "F", Role = UserRole.Student,
            AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act - 5 consecutive wrong password attempts
        for (int i = 0; i < 5; i++)
        {
            try { await service.LoginAsync(new LoginRequest("bruteforce@test.com", "WrongPass@1")); }
            catch { /* expected */ }
        }

        // Assert - account should now be locked
        var user = await db.Users.FindAsync(userId);
        user!.AccountStatus.Should().Be(AccountStatus.Locked,
            "5 consecutive failed login attempts must lock the account");
    }

    [Fact]
    public async Task Login_FailedAttempts_ShouldIncrementCounter()
    {
        // Arrange
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Email = "counter@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1"),
            FirstName = "C", LastName = "T", Role = UserRole.Student,
            AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act - 2 wrong attempts
        for (int i = 0; i < 2; i++)
        {
            try { await service.LoginAsync(new LoginRequest("counter@test.com", "WrongPass@1")); }
            catch { /* expected */ }
        }

        // Assert
        var user = await db.Users.FindAsync(userId);
        user!.FailedLoginAttempts.Should().Be(2);
    }

    // ─── Password Reset Token Security ───────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "expiredtoken@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@1"),
            FirstName = "E", LastName = "T", Role = UserRole.Student,
            AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTime.UtcNow,
            ResetPasswordCode = "EXPIRED123",
            ResetPasswordCodeExpiresAt = DateTime.UtcNow.AddHours(-1), // expired 1 hour ago
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Func<Task> act = () => service.ResetPasswordAsync(
            new ResetPasswordRequest("expiredtoken@test.com", "EXPIRED123", "NewPass@1234"));

        await act.Should().ThrowAsync<Exception>("expired reset tokens must be rejected");
    }

    [Fact]
    public async Task ResetPassword_WithWrongToken_ShouldThrow()
    {
        // Arrange
        using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "wrongtoken@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@1"),
            FirstName = "W", LastName = "T", Role = UserRole.Student,
            AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTime.UtcNow,
            ResetPasswordCode = "CORRECT123",
            ResetPasswordCodeExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Func<Task> act = () => service.ResetPasswordAsync(
            new ResetPasswordRequest("wrongtoken@test.com", "WRONG999", "NewPass@1234"));

        await act.Should().ThrowAsync<Exception>("wrong reset tokens must be rejected");
    }

    // ─── Successful Login Resets Failed Counter ───────────────────────────────

    [Fact]
    public async Task Login_AfterSuccess_ShouldResetFailedAttemptCounter()
    {
        // Arrange
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId, Email = "resetcounter@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1"),
            FirstName = "R", LastName = "C", Role = UserRole.Student,
            AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTime.UtcNow,
            FailedLoginAttempts = 3, // had previous failures
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act - login with correct password
        await service.LoginAsync(new LoginRequest("resetcounter@test.com", "CorrectPass@1"));

        // Assert
        var user = await db.Users.FindAsync(userId);
        user!.FailedLoginAttempts.Should().Be(0,
            "a successful login must reset the failed attempts counter");
    }
}
