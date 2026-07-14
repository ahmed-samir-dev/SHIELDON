using SHIELDON.Application.Features.Chat.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Defines the contract for storing and serving uploaded chat attachment files.
/// Enforces file size limits and restricts allowed file types.
/// Implementation saves files to wwwroot/uploads/chat/{type}/ on the API server.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Validates and persists an uploaded attachment file.
    /// </summary>
    /// <param name="fileStream">The raw file content stream.</param>
    /// <param name="originalFileName">The original file name (used to determine extension and type).</param>
    /// <param name="contentType">MIME type from the HTTP request (used for additional validation).</param>
    /// <returns>
    /// An <see cref="AttachmentUploadResponse"/> containing the relative URL and resolved attachment type.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the file exceeds 10MB or uses a disallowed extension.
    /// </exception>
    Task<AttachmentUploadResponse> SaveChatAttachmentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType);

    /// <summary>
    /// Deletes a previously stored attachment file by its relative URL path.
    /// Safe to call with null or empty — will silently no-op.
    /// </summary>
    Task DeleteAsync(string? relativeUrl);
}
