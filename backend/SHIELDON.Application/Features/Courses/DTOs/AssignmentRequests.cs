namespace SHIELDON.Application.Features.Courses.DTOs;

// ── Assignment Requests ────────────────────────────────────────────────────

/// <summary>
/// Request to create a new assignment in a course.
/// An optional reference file (problem sheet, rubric, etc.) is sent as a separate
/// IFormFile via multipart/form-data — not included in this record.
/// </summary>
public record CreateAssignmentRequest(
    string Title,
    string? Instructions,
    DateTime? DueDate
);

/// <summary>
/// Request to update an existing assignment's metadata (title, instructions, due date).
/// The reference file cannot be changed here — use the dedicated replace-reference endpoint
/// or delete and re-create the assignment.
/// </summary>
public record UpdateAssignmentRequest(
    string Title,
    string? Instructions,
    DateTime? DueDate
);
