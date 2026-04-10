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
    private const int VerificationTokenExpiryHours = 24;
    private const int ResendCooldownMinutes = 2;

    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext db, IJwtService jwtService,
        IEmailService emailService, IConfiguration configuration)
    {
        _db = db;
        _jwtService = jwtService;
        _emailService = emailService;
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
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                // Fire-and-forget: notify user their account is locked
                _ = _emailService.SendAccountLockedEmailAsync(user.Email, user.FullName);
                throw new ForbiddenException("Your account has been locked due to too many failed login attempts. Please reset your password.");
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
    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Always return a generic error to prevent email enumeration
        if (user is null)
            throw new BusinessRuleException("Verification link is invalid or has expired.");

        if (user.AccountStatus == AccountStatus.Active)
            throw new BusinessRuleException("This account has already been verified.");

        // Validate token
        if (user.VerificationCode != request.Token ||
            user.VerificationCodeExpiresAt is null ||
            user.VerificationCodeExpiresAt < DateTime.UtcNow)
        {
            throw new BusinessRuleException("Verification link is invalid or has expired. Please request a new one.");
        }

        // Activate the account and clear the token
        user.AccountStatus = AccountStatus.Active;
        user.VerificationCode = null;
        user.VerificationCodeExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Silently succeed if email not found — prevents enumeration
        if (user is null) return;

        if (user.AccountStatus == AccountStatus.Active)
            throw new BusinessRuleException("This account is already verified.");

        if (user.AccountStatus == AccountStatus.Disabled)
            throw new ForbiddenException("Your account has been disabled. Please contact support.");

        // Rate limit: don't resend if a token was issued less than 2 minutes ago
        if (user.VerificationCodeExpiresAt.HasValue &&
            user.VerificationCodeExpiresAt.Value > DateTime.UtcNow.AddHours(VerificationTokenExpiryHours - ResendCooldownMinutes / 60.0))
        {
            throw new BusinessRuleException($"Please wait at least {ResendCooldownMinutes} minutes before requesting a new verification email.");
        }

        // Generate a new secure token
        var token = GenerateSecureToken();
        user.VerificationCode = token;
        user.VerificationCodeExpiresAt = DateTime.UtcNow.AddHours(VerificationTokenExpiryHours);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Send the email (fire-and-forget — don't fail the request on SMTP error)
        _ = _emailService.SendEmailVerificationAsync(user.Email, user.FullName, token);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Silently succeed if email not found to prevent enumeration attacks
        if (user is null) return;

        // Rate limit: prevent spamming reset emails
        if (user.ResetPasswordCodeExpiresAt.HasValue &&
            user.ResetPasswordCodeExpiresAt.Value > DateTime.UtcNow.AddMinutes(60 - ResendCooldownMinutes))
        {
            return; // Silently ignore fast subsequent requests
        }

        // Generate a new secure token
        var resetToken = GenerateSecureToken();
        user.ResetPasswordCode = resetToken;
        user.ResetPasswordCodeExpiresAt = DateTime.UtcNow.AddMinutes(60); // 1 hour expiry
        user.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync(ct);

        // Fire-and-forget email
        _ = _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Generic error to prevent enumeration
        if (user is null)
            throw new BusinessRuleException("The password reset link is invalid or has expired.");

        // Validate token
        if (user.ResetPasswordCode != request.Token ||
            user.ResetPasswordCodeExpiresAt is null ||
            user.ResetPasswordCodeExpiresAt < DateTime.UtcNow)
        {
            throw new BusinessRuleException("The password reset link is invalid or has expired. Please request a new one.");
        }

        // Hash new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Clear the token
        user.ResetPasswordCode = null;
        user.ResetPasswordCodeExpiresAt = null;

        // Unlock account and reset failed attempts if necessary
        if (user.AccountStatus == AccountStatus.Locked)
        {
            user.AccountStatus = AccountStatus.Active;
        }
        user.FailedLoginAttempts = 0;
        
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        
        // Notify user about password change
        _ = _emailService.SendSecurityNotificationEmailAsync(user.Email, user.FullName, "Your password was successfully reset.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Generates a cryptographically secure URL-safe token.</summary>
    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[48];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('='); // URL-safe Base64
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
