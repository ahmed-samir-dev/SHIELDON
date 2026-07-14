using SHIELDON.Application.Features.Chat.DTOs;
using SHIELDON.Application.Interfaces;
using SHIELDON.Domain.Enums;

namespace SHIELDON.API.Services;

/// <summary>
/// Saves chat attachment files (voice notes, images, documents) to the local wwwroot.
/// Enforces a 10MB size limit and restricts accepted file extensions to a safe allow-list.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly Dictionary<string, AttachmentType> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        { ".pdf",  AttachmentType.Document },
        { ".docx", AttachmentType.Document },
        { ".pptx", AttachmentType.Document },
        // Images
        { ".jpg",  AttachmentType.Image },
        { ".jpeg", AttachmentType.Image },
        { ".png",  AttachmentType.Image },
        // Audio / Voice notes
        { ".mp3",  AttachmentType.Audio },
        { ".wav",  AttachmentType.Audio },
        { ".webm", AttachmentType.Audio },
        { ".ogg",  AttachmentType.Audio },
        { ".m4a",  AttachmentType.Audio }
    };

    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <inheritdoc />
    public async Task<AttachmentUploadResponse> SaveChatAttachmentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType)
    {
        // ── Validate file size ──────────────────────────────────────────────────
        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File size exceeds the 10MB limit.");

        // ── Validate extension ──────────────────────────────────────────────────
        var ext = Path.GetExtension(originalFileName);
        if (!AllowedExtensions.TryGetValue(ext, out var attachmentType))
            throw new InvalidOperationException(
                $"File type '{ext}' is not allowed. Accepted types: PDF, DOCX, PPTX, JPG, PNG, MP3, WAV, WEBM, OGG, M4A.");

        // ── Resolve storage subfolder ──────────────────────────────────────────
        var subFolder = attachmentType switch
        {
            AttachmentType.Document => "documents",
            AttachmentType.Image    => "images",
            AttachmentType.Audio    => "audio",
            _                      => "misc"
        };

        var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", "chat", subFolder);
        Directory.CreateDirectory(uploadRoot); // Idempotent

        // ── Save with a unique filename to prevent collisions ──────────────────
        var safeOriginalName = Path.GetFileNameWithoutExtension(originalFileName);
        // Replace spaces and special characters with hyphens
        safeOriginalName = System.Text.RegularExpressions.Regex.Replace(safeOriginalName, @"[^a-zA-Z0-9_-]", "-");
        safeOriginalName = safeOriginalName.Trim('-');
        if (string.IsNullOrWhiteSpace(safeOriginalName))
            safeOriginalName = "file";
        
        var fileName = $"{safeOriginalName}_{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadRoot, fileName);

        await using var fileOutput = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileOutput);

        // ── Return relative URL for storage in the database ────────────────────
        var relativeUrl = $"/uploads/chat/{subFolder}/{fileName}";

        return new AttachmentUploadResponse
        {
            Url = relativeUrl,
            AttachmentType = attachmentType
        };
    }

    /// <inheritdoc />
    public Task DeleteAsync(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return Task.CompletedTask;

        var fullPath = Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
