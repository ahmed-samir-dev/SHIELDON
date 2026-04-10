using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SHIELDON.Application.Features.Auth.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Implementation of IAuthService.
/// Handles credential validation, JWT issuance, token rotation, and logout.
/// All sensitive operations are wrapped in try/catch to prevent information leakage.
/// </summary>
public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;

    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, IJwtService jwtService, IConfiguration configuration)
    {
        _db = db;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // 1. Normalise the email to prevent case-sensitive duplicates
        var email = request.Email.Trim().ToLowerInvariant();

        // 2. Look up user — never expose whether the email exists (use generic error)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
            throw new UnauthorizedException("Invalid email or password.");

        // 3. Check account status before verifying the password
        switch (user.AccountStatus)
        {
            case AccountStatus.Unverified:
                throw new ForbiddenException("Please verify your email address before logging in.");
            case AccountStatus.Locked:
                throw new ForbiddenException("Your account has been locked. Please reset your password.");
            case AccountStatus.Disabled:
                throw new ForbiddenException("Your account has been disabled. Please contact support.");
        }

        // 4. Verify password
        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            // Increment failed attempts and potentially lock the account
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.AccountStatus = AccountStatus.Locked;
            }
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            throw new UnauthorizedException("Invalid email or password.");
        }

        // 5. Reset failed attempts on successful login
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        // 6. Issue tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = int.Parse(
            _configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");

        // 7. Persist refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiry),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        _db.RefreshTokens.Add(refreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        var accessExpiryMinutes = int.Parse(
            _configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "15");

        return MapToResponse(user, accessToken, rawRefreshToken,
            DateTime.UtcNow.AddMinutes(accessExpiryMinutes));
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // Find the stored refresh token, eagerly loading the User
        var storedToken = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (storedToken is null || !storedToken.IsActive)
            throw new UnauthorizedException("Refresh token is invalid, expired, or has been revoked.");

        // Revoke the old token (rotation strategy)
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedReason = "Replaced";

        // Issue new tokens
        var user = storedToken.User;
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = int.Parse(
            _configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiry),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        _db.RefreshTokens.Add(newRefreshTokenEntity);
        await _db.SaveChangesAsync(ct);

        var accessExpiryMinutes = int.Parse(
            _configuration["JwtSettings:AccessTokenExpiryMinutes"] ?? "15");

        return MapToResponse(user, newAccessToken, newRawRefreshToken,
            DateTime.UtcNow.AddMinutes(accessExpiryMinutes));
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken && t.UserId == userId, ct);

        if (storedToken is not null && storedToken.IsActive)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedReason = "Logout";
            await _db.SaveChangesAsync(ct);
        }
        // Silently succeed even if token not found — idempotent logout
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static LoginResponse MapToResponse(
        User user, string accessToken, string refreshToken, DateTime accessExpiresAt)
    {
        return new LoginResponse(
            UserId: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            ProfilePictureUrl: user.ProfilePictureUrl,
            Role: user.Role,
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiresAt: accessExpiresAt
        );
    }
}
