using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Handles file storage for profile pictures and course materials.
/// Files are stored under wwwroot/uploads/ and NEVER served directly from disk.
/// All downloads go through an API controller that validates permissions first.
/// </summary>
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    private static readonly HashSet<string> AllowedProfilePictureTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private static readonly HashSet<string> AllowedMaterialTypes =
        ["application/pdf", "application/msword",
         "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
         "application/vnd.ms-powerpoint",
         "application/vnd.openxmlformats-officedocument.presentationml.presentation",
         "image/jpeg", "image/png"];

    public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> SaveProfilePictureAsync(
        Stream fileStream, string contentType, string originalFileName, Guid userId)
    {
        ValidateMimeType(contentType, AllowedProfilePictureTypes);

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var folder = Path.Combine(_env.WebRootPath, "uploads", "profile-pictures");
        Directory.CreateDirectory(folder);

        // Delete existing profile picture for this user (any extension)
        foreach (var existing in Directory.GetFiles(folder, $"{userId}.*"))
            File.Delete(existing);

        var fileName = $"{userId}{extension}";
        var filePath = Path.Combine(folder, fileName);

        await using var outputStream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        // Return relative path stored in DB
        return $"uploads/profile-pictures/{fileName}";
    }

    /// <inheritdoc />
    public async Task<string> SaveCourseMaterialAsync(
        Stream fileStream, string contentType, string originalFileName, Guid courseId)
    {
        ValidateMimeType(contentType, AllowedMaterialTypes);

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var safeFileName = Path.GetFileNameWithoutExtension(originalFileName)
            .Replace(" ", "_")
            .Replace("..", ""); // Prevent path traversal

        var folder = Path.Combine(_env.WebRootPath, "uploads", "course-materials", courseId.ToString());
        Directory.CreateDirectory(folder);

        var uniqueFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{safeFileName}{extension}";
        var filePath = Path.Combine(folder, uniqueFileName);

        await using var outputStream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(outputStream);

        return $"uploads/course-materials/{courseId}/{uniqueFileName}";
    }

    /// <inheritdoc />
    public void DeleteFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        var physicalPath = GetPhysicalPath(relativePath);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
            _logger.LogInformation("Deleted file: {Path}", relativePath);
        }
    }

    /// <inheritdoc />
    public string GetPhysicalPath(string relativePath)
        => Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static void ValidateMimeType(string contentType, HashSet<string> allowed)
    {
        if (!allowed.Contains(contentType.ToLowerInvariant()))
            throw new InvalidOperationException($"File type '{contentType}' is not allowed.");
    }
}
