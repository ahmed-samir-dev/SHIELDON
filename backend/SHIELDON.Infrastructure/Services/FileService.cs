using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace SHIELDON.Infrastructure.Services;

/// <summary>
/// Handles file storage for profile pictures and course materials.
/// Files are securely stored outside of wwwroot in an absolute Storage/ folder 
/// and NEVER served directly from disk by IIS/Kestrel.
/// All downloads go through the FilesController that validates route permissions first.
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

        var folder = Path.Combine(_env.ContentRootPath, "Storage", "Uploads", "profile-pictures");
        Directory.CreateDirectory(folder);

        // Delete existing profile picture for this user (any extension, though we now standardize on webp)
        foreach (var existing in Directory.GetFiles(folder, $"{userId}.*"))
            File.Delete(existing);

        var fileName = $"{userId}.webp";
        var filePath = Path.Combine(folder, fileName);

        // Load the image, resize/crop exactly to 200x200, and save as optimized WebP
        using var image = await Image.LoadAsync(fileStream);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(200, 200),
            Mode = ResizeMode.Crop
        }));

        await using var outputStream = new FileStream(filePath, FileMode.Create);
        await image.SaveAsync(outputStream, new WebpEncoder { Quality = 85 });

        // Return relative path stored in DB (for routing)
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

        var folder = Path.Combine(_env.ContentRootPath, "Storage", "Uploads", "course-materials", courseId.ToString());
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
    {
        // relativePath will look like "uploads/profile-pictures/guid.webp"
        // We map "uploads/..." to "Storage/Uploads/..." relative to ContentRoot
        
        var normalizedPath = relativePath.Replace("uploads/", "", StringComparison.OrdinalIgnoreCase);
        return Path.Combine(_env.ContentRootPath, "Storage", "Uploads", normalizedPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void ValidateMimeType(string contentType, HashSet<string> allowed)
    {
        if (!allowed.Contains(contentType.ToLowerInvariant()))
            throw new InvalidOperationException($"File type '{contentType}' is not allowed.");
    }
}
