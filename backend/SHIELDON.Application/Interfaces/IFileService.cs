namespace SHIELDON.Application.Interfaces;

/// <summary>
/// Contract for storing and retrieving files on the server.
/// Implemented in SHIELDON.Infrastructure/Services/FileService.cs.
/// Files are NEVER served directly from disk — always through an API controller.
/// Note: Uses Stream instead of IFormFile to keep Application layer free from ASP.NET Core dependency.
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Saves an uploaded profile picture to wwwroot/uploads/profile-pictures/{userId}.{ext}.
    /// Deletes the old picture if one exists.
    /// Returns the relative path stored in the database.
    /// </summary>
    Task<string> SaveProfilePictureAsync(Stream fileStream, string contentType, string originalFileName, Guid userId);

    /// <summary>
    /// Saves a course material file to wwwroot/uploads/course-materials/{courseId}/{timestamp}_{filename}.
    /// Returns the relative path stored in the database.
    /// </summary>
    Task<string> SaveCourseMaterialAsync(Stream fileStream, string contentType, string originalFileName, Guid courseId);

    /// <summary>
    /// Deletes a file at the given relative path.
    /// Does nothing if the file doesn't exist (safe to call).
    /// </summary>
    void DeleteFile(string relativePath);

    /// <summary>
    /// Returns the physical (absolute) path for a given relative path.
    /// Used internally by the file serving controller.
    /// </summary>
    string GetPhysicalPath(string relativePath);
}
