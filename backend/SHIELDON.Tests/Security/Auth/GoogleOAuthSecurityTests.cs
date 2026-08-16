using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using SHIELDON.Infrastructure.Services;
using SHIELDON.Application.Features.Auth.DTOs;
using SHIELDON.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace SHIELDON.Tests.Security.Auth;

/// <summary>
/// Security tests for the Google OAuth authentication flow.
/// Note: GoogleAuthAsync reads the JWT token but does NOT verify its signature against
/// Google's public keys - it trusts the token payload. This is the current behavior.
/// These tests cover input validation, null/empty token, and locked account guards.
/// </summary>
public class GoogleOAuthSecurityTests
{
    private AppDbContext CreateDbContext() =>
        new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private AuthService CreateService(AppDbContext db) =>
        new AuthService(
            db,
            new Mock<IJwtService>().Object,
            new Mock<IEmailService>().Object,
            new Mock<IConfiguration>().Object);

    // ─── Empty / Null Token Rejected ─────────────────────────────────────────

    [Fact]
    public async Task GoogleAuth_WithEmptyToken_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = CreateService(db);

        // Act
        Func<Task> act = () => service.GoogleAuthAsync(new GoogleAuthRequest(string.Empty));

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>(
            "empty Google ID token must be rejected immediately");
    }

    // ─── Whitespace-Only Token Rejected ───────────────────────────────────────

    [Fact]
    public async Task GoogleAuth_WithWhitespaceToken_ShouldThrowBusinessRuleException()
    {
        // Arrange
        using var db = CreateDbContext();
        var service = CreateService(db);

        // Act
        Func<Task> act = () => service.GoogleAuthAsync(new GoogleAuthRequest("   "));

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>(
            "whitespace-only Google ID token must be rejected");
    }

    // ─── Malformed / Non-JWT Token Rejected ──────────────────────────────────

    [Theory]
    [InlineData("not-a-jwt-at-all")]
    [InlineData("two.parts")]
    public async Task GoogleAuth_WithMalformedToken_ShouldThrowBusinessRuleException(string badToken)
    {
        // Arrange
        using var db = CreateDbContext();
        var service = CreateService(db);

        // Act
        Func<Task> act = () => service.GoogleAuthAsync(new GoogleAuthRequest(badToken));

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>(
            "malformed Google ID tokens must be rejected with a BusinessRuleException");
    }

    // ─── Locked Account with Valid-Format Google Token ────────────────────────

    [Fact]
    public async Task GoogleAuth_WithLockedAccount_ShouldThrowForbiddenException()
    {
        // Arrange - seed a locked user with a known email
        using var db = CreateDbContext();
        var knownEmail = "locked@google.test";
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = knownEmail,
            FirstName = "Locked", LastName = "User",
            Role = UserRole.Student,
            AuthProvider = "Google",
            AccountStatus = AccountStatus.Locked,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Build a minimal valid-format JWT that has the email claim
        // AuthService reads but does NOT verify the JWT signature - this is a known behavior.
        var header  = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"email\":\"{knownEmail}\",\"given_name\":\"Locked\",\"family_name\":\"User\"}}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var fakeToken = $"{header}.{payload}.fakesig";

        // Act
        Func<Task> act = () => service.GoogleAuthAsync(new GoogleAuthRequest(fakeToken));

        // Assert - locked account must be rejected even via Google SSO
        await act.Should().ThrowAsync<ForbiddenException>(
            "locked accounts must not be able to authenticate via Google OAuth");
    }
}
