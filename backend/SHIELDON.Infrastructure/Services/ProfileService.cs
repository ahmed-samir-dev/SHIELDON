using System.Security.Cryptography;
using SHIELDON.Application.Features.Users.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Exceptions;
using SHIELDON.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SHIELDON.Domain.Entities;
using SHIELDON.Domain.Enums;

namespace SHIELDON.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IFileService _fileService;
    private readonly IOtpService _otpService;

    /// <summary>Maximum failed OTP attempts before the code is invalidated and a new send is required.</summary>
    private const int MAX_OTP_ATTEMPTS = 5;

    /// <summary>Minimum seconds between OTP resend requests (2-minute cooldown).</summary>
    private const int OTP_RESEND_COOLDOWN_MINUTES = 2;

    /// <summary>Minutes before an OTP code expires after being sent.</summary>
    private const int OTP_EXPIRY_MINUTES = 10;

    public ProfileService(AppDbContext db, IFileService fileService, IOtpService otpService)
    {
        _db = db;
        _fileService = fileService;
        _otpService = otpService;
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        var displayId = user.Role switch
        {
            UserRole.Admin => user.AdminId,
            UserRole.Tutor => user.TutorId,
            UserRole.Student => user.StudentId,
            _ => null
        };

        return new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.ProfilePictureUrl,
            user.Role,
            displayId,
            user.AccountStatus,
            user.CreatedAt,
            user.HasPassword,
            user.PhoneNumber,
            user.PhoneVerificationStatus.ToString(),
            user.PhoneVerifiedAt
        );
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        RecordActivityLog(userId, "ProfileUpdate");
        await _db.SaveChangesAsync(ct);

        return await GetProfileAsync(userId, ct);
    }

    public async Task<UserProfileResponse> UploadProfilePictureAsync(
        Guid userId, Stream fileStream, string contentType, string originalFileName, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        // Hand off to the FileService, which handles ImageSharp resizing and webp conversion
        var relativePath = await _fileService.SaveProfilePictureAsync(fileStream, contentType, originalFileName, userId);

        user.ProfilePictureUrl = relativePath;
        user.UpdatedAt = DateTime.UtcNow;

        RecordActivityLog(userId, "PictureUpload");
        await _db.SaveChangesAsync(ct);

        return await GetProfileAsync(userId, ct);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        bool isPasswordless = !user.HasPassword;

        if (!isPasswordless)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new BusinessRuleException("Incorrect current password.");
            }
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        RecordActivityLog(userId, "PasswordChange");
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task CompleteOnboardingAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        user.HasCompletedOnboarding = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetOnboardingAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        user.HasCompletedOnboarding = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UserProfileResponse> UpdatePhoneAsync(Guid userId, UpdatePhoneRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        var phone = request.PhoneNumber.Trim();
        if (phone.StartsWith("+200"))
        {
            phone = "+20" + phone[4..];
        }

        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone && u.Id != userId, ct);
        if (existingUser != null)
        {
            throw new BusinessRuleException("This phone number is already registered to another account.");
        }

        user.PhoneNumber = phone;
        user.PhoneVerificationStatus = PhoneVerificationStatus.Unverified;
        user.PhoneVerifiedAt = null;
        user.PhoneOtpLastSentAt = null;
        user.PhoneOtpExpiresAt = null;
        user.PhoneOtpCodeHash = null;
        user.PhoneOtpFailedAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        RecordActivityLog(userId, "PhoneUpdate");
        await _db.SaveChangesAsync(ct);

        return await GetProfileAsync(userId, ct);
    }

    public async Task SendPhoneOtpAsync(Guid userId, SendPhoneOtpRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            throw new BusinessRuleException("Please save a phone number first before requesting an OTP code.");
        }

        // Enforce 2-minute resend cooldown
        if (user.PhoneOtpLastSentAt.HasValue &&
            DateTime.UtcNow < user.PhoneOtpLastSentAt.Value.AddMinutes(OTP_RESEND_COOLDOWN_MINUTES))
        {
            var remainingSeconds = (int)(user.PhoneOtpLastSentAt.Value.AddMinutes(OTP_RESEND_COOLDOWN_MINUTES) - DateTime.UtcNow).TotalSeconds;
            throw new BusinessRuleException($"Please wait {remainingSeconds} seconds before requesting a new OTP code.");
        }

        // Generate a cryptographically secure 6-digit OTP code
        var otpCode = GenerateOtpCode();

        // Hash the code before storing — never persist plaintext OTP
        user.PhoneOtpCodeHash = BCrypt.Net.BCrypt.HashPassword(otpCode);
        user.PhoneOtpLastSentAt = DateTime.UtcNow;
        user.PhoneOtpExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);
        user.PhoneOtpFailedAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        // Persist hash BEFORE delivering message — ensures the hash exists if delivery is instantaneous
        RecordActivityLog(userId, "PhoneOtpSent_whatsapp");
        await _db.SaveChangesAsync(ct);

        // Deliver the plaintext code to the user's WhatsApp via the self-hosted gateway
        await _otpService.SendOtpAsync(user.PhoneNumber, otpCode, ct);
    }

    public async Task<UserProfileResponse> VerifyPhoneOtpAsync(Guid userId, VerifyPhoneOtpRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User Profile", userId);

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            throw new BusinessRuleException("No phone number registered for verification.");
        }

        if (string.IsNullOrWhiteSpace(user.PhoneOtpCodeHash))
        {
            throw new BusinessRuleException("No active OTP code found. Please request a new one first.");
        }

        // Check expiry before attempt count to give user accurate feedback
        if (user.PhoneOtpExpiresAt.HasValue && DateTime.UtcNow > user.PhoneOtpExpiresAt.Value)
        {
            // Clear expired state
            user.PhoneOtpCodeHash = null;
            user.PhoneOtpExpiresAt = null;
            await _db.SaveChangesAsync(ct);
            throw new BusinessRuleException("OTP code has expired. Please request a new one.");
        }

        // Check attempt lockout
        if (user.PhoneOtpFailedAttempts >= MAX_OTP_ATTEMPTS)
        {
            user.PhoneOtpCodeHash = null;
            user.PhoneOtpLastSentAt = null;
            user.PhoneOtpExpiresAt = null;
            await _db.SaveChangesAsync(ct);
            throw new BusinessRuleException("Too many failed OTP verification attempts. Please request a new code.");
        }

        // Verify the submitted code against the stored BCrypt hash
        bool isValid = BCrypt.Net.BCrypt.Verify(request.Code, user.PhoneOtpCodeHash);

        if (!isValid)
        {
            user.PhoneOtpFailedAttempts++;
            await _db.SaveChangesAsync(ct);
            int remaining = MAX_OTP_ATTEMPTS - user.PhoneOtpFailedAttempts;
            throw new BusinessRuleException($"Invalid OTP code. {remaining} attempt(s) remaining.");
        }

        // Verification successful — update user state and clear OTP fields
        user.PhoneVerificationStatus = PhoneVerificationStatus.Verified;
        user.PhoneVerifiedAt = DateTime.UtcNow;
        user.PhoneOtpCodeHash = null;
        user.PhoneOtpLastSentAt = null;
        user.PhoneOtpExpiresAt = null;
        user.PhoneOtpFailedAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        RecordActivityLog(userId, "PhoneVerified");
        await _db.SaveChangesAsync(ct);

        return await GetProfileAsync(userId, ct);
    }

    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP code using RandomNumberGenerator.
    /// Range: 100000–999999 (always exactly 6 digits, never starts with 0).
    /// </summary>
    private static string GenerateOtpCode()
    {
        // RandomNumberGenerator produces an unbiased random integer in the given range
        var code = RandomNumberGenerator.GetInt32(100000, 1000000);
        return code.ToString();
    }

    private void RecordActivityLog(Guid userId, string eventType)
    {
        var log = new UserActivityLog
        {
            UserId = userId,
            EventType = eventType,
            CreatedAt = DateTime.UtcNow,
            IpAddress = "127.0.0.1" // Placeholder
        };
        _db.UserActivityLogs.Add(log);
    }
}
