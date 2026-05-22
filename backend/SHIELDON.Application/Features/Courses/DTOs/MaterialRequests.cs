namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Material Requests ──────────────────────────────────────────────────────

/// <summary>
/// Request to add a new material to a course.
/// MaterialType = "File" requires the physical IFormFile to be sent via multipart/form-data.
/// MaterialType = "Link" requires ExternalUrl.
/// </summary>
public record AddMaterialRequest(
    string Title,
    string? Description,
    string MaterialType,       // "File" or "Link"
    string? ExternalUrl        // Required when MaterialType = "Link"
);

/// <summary>
/// Request to update the metadata (title, description) of an existing material.
/// File bytes and URLs cannot be changed after upload - delete and re-upload instead.
/// </summary>
public record UpdateMaterialRequest(
    string Title,
    string? Description
);
