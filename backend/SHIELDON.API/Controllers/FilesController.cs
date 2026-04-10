using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Controller responsible for securely serving files that are stored outside of wwwroot.
/// This prevents arbitrary file access and ensures all files go through ASP.NET Core authorization (if applied).
/// </summary>
[ApiController]
[Route("uploads")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    /// <summary>
    /// GET /uploads/profile-pictures/{filename}
    /// Serves profile pictures. Currently open to all users (AllowAnonymous),
    /// but can be locked down if user privacy requires it.
    /// </summary>
    [HttpGet("profile-pictures/{filename}")]
    [AllowAnonymous]
    public IActionResult GetProfilePicture(string filename)
    {
        var relativePath = $"uploads/profile-pictures/{filename}";
        var physicalPath = _fileService.GetPhysicalPath(relativePath);

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        // Since we explicitly convert all profile pictures to WebP in Stage 1.5,
        // we can hardcode the content type, but it's safer to map it.
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(physicalPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(physicalPath, contentType);
    }

    /// <summary>
    /// GET /uploads/course-materials/{courseId}/{filename}
    /// Serves course materials.
    /// FUTURE PHASE: Protect with course enrollment verification!
    /// </summary>
    [HttpGet("course-materials/{courseId}/{filename}")]
    [Authorize] // Requires login
    public IActionResult GetCourseMaterial(Guid courseId, string filename)
    {
        // TODO: Validate that the current user is enrolled in the requested courseId
        
        var relativePath = $"uploads/course-materials/{courseId}/{filename}";
        var physicalPath = _fileService.GetPhysicalPath(relativePath);

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(physicalPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(physicalPath, contentType);
    }
}
