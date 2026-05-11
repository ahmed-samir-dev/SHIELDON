using SHIELDON.Application.Common;
using SHIELDON.Application.Features.Courses.DTOs;
using SHIELDON.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SHIELDON.API.Controllers;

/// <summary>
/// Manages course material uploads, listings, downloads, and deletions.
/// - Admin / assigned Tutor: upload, delete
/// - Admin / Tutor / Approved Student: list, download
/// All endpoints require JWT authentication.
/// </summary>
[ApiController]
[Route("api/courses/{courseId:guid}/materials")]
[Authorize]
public class MaterialsController : ControllerBase
{
    private readonly IMaterialService _materialService;

    public MaterialsController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    // ── Upload / Add ──────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/courses/{courseId}/materials
    /// Upload a file (multipart/form-data) or register an external link.
    /// Admin or assigned Tutor only.
    /// Form fields: title, description, materialType ("File"|"Link"), externalUrl (if Link), file (if File).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Tutor")]
    [RequestSizeLimit(105_000_000)] // 100 MB + headers overhead
    [ProducesResponseType(typeof(ApiResponse<MaterialResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddMaterial(
        Guid courseId,
        [FromForm] string title,
        [FromForm] string materialType,
        [FromForm] string? description = null,
        [FromForm] string? externalUrl = null,
        IFormFile? file = null,
        CancellationToken cancellationToken = default)
    {
        var request = new AddMaterialRequest(title, description, materialType, externalUrl);

        // Adapt IFormFile → UploadedFileDto to keep Application layer framework-agnostic
        UploadedFileDto? uploadedFile = file is not null
            ? new UploadedFileDto(file.OpenReadStream(), file.FileName, file.ContentType, file.Length)
            : null;

        var result = await _materialService.AddMaterialAsync(
            courseId, request, uploadedFile, GetUserId(), GetUserRole(), cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<MaterialResponse>.Ok(result, "Material added successfully."));
    }

    // ── List ──────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/courses/{courseId}/materials
    /// Lists all materials for a course.
    /// Students: must be Approved-enrolled.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MaterialResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMaterials(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await _materialService.GetMaterialsAsync(
            courseId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<MaterialResponse>>.Ok(result, "Materials retrieved successfully."));
    }

    // ── Download ──────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/courses/{courseId}/materials/{materialId}/download
    /// Streams the physical file to the client as an attachment.
    /// Students: must be Approved-enrolled. Links cannot be downloaded via this endpoint.
    /// </summary>
    [HttpGet("{materialId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadMaterial(
        Guid courseId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        var (stream, contentType, fileName) = await _materialService.DownloadMaterialAsync(
            materialId, GetUserId(), GetUserRole(), cancellationToken);

        return File(stream, contentType, fileName);
    }

    // ── Delete ────────────────────────────────────────────────────────────

    /// <summary>
    /// DELETE /api/courses/{courseId}/materials/{materialId}
    /// Deletes a material record and removes the physical file (if applicable).
    /// Admin or assigned Tutor only.
    /// </summary>
    [HttpDelete("{materialId:guid}")]
    [Authorize(Roles = "Admin,Tutor")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMaterial(
        Guid courseId,
        Guid materialId,
        CancellationToken cancellationToken)
    {
        await _materialService.DeleteMaterialAsync(
            materialId, GetUserId(), GetUserRole(), cancellationToken);

        return Ok(ApiResponse<object>.Ok("Material deleted successfully."));
    }
}
