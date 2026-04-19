using SHIELDON.Application.Features.Courses.DTOs;

namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Framework-agnostic representation of an uploaded file.
/// Used so the Application layer interface does not reference Microsoft.AspNetCore.Http (IFormFile).
/// The API controller adapter creates this from the IFormFile before calling the service.
/// </summary>
public record UploadedFileDto(
    Stream Content,
    string FileName,
    string ContentType,
    long Length
);

/// <summary>
/// Material management service contract.
/// Handles file upload, external link registration, listing, and deletion for course materials.
/// Access rules enforced here:
/// - Only Admin or the assigned Tutor may upload/delete.
/// - Only enrolled (Approved) students may download.
/// </summary>
public interface IMaterialService
{
    /// <summary>
    /// Uploads a physical file or registers an external link as a course material.
    /// Validates MIME type and file size before writing to disk.
    /// </summary>
    Task<MaterialResponse> AddMaterialAsync(
        Guid courseId,
        AddMaterialRequest request,
        UploadedFileDto? file,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all materials for a course.
    /// Students: must be Approved-enrolled. Admin/Tutor: always allowed.
    /// </summary>
    Task<IReadOnlyList<MaterialResponse>> GetMaterialsAsync(
        Guid courseId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the physical file stream and metadata so it can be served as a download.
    /// Students: must be Approved-enrolled. Admin/Tutor: always allowed.
    /// </summary>
    Task<(Stream FileStream, string ContentType, string FileName)> DownloadMaterialAsync(
        Guid materialId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a material record and its physical file (if any).
    /// Only Admin or the assigned Tutor of the course may delete.
    /// </summary>
    Task DeleteMaterialAsync(
        Guid materialId,
        Guid requestingUserId,
        string requestingUserRole,
        CancellationToken ct = default);
}
