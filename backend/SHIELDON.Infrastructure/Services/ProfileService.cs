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

    public ProfileService(AppDbContext db, IFileService fileService)
    {
        _db = db;
        _fileService = fileService;
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
            user.CreatedAt
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

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessRuleException("Incorrect current password.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        RecordActivityLog(userId, "PasswordChange");
        await _db.SaveChangesAsync(ct);

        return true;
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
