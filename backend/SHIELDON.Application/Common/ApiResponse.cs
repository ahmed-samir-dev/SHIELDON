namespace SHIELDON.Application.Common;

/// <summary>
/// The unified API response envelope used by every endpoint in SHIELDON.
/// Every response — success or error — wraps its data in this format.
/// This ensures the frontend always receives a predictable, typed structure.
/// </summary>
/// <typeparam name="T">The type of the response data payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>True if the operation succeeded; false otherwise.</summary>
    public bool Success { get; set; }

    /// <summary>The response data payload. Null on error.</summary>
    public T? Data { get; set; }

    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; set; }

    /// <summary>
    /// A list of validation or error messages.
    /// Empty on success; populated on validation failure (400) or business rule errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; set; } = [];

    // ── Factory Methods ───────────────────────────────────────
    /// <summary>Creates a successful response with data and optional message.</summary>
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    /// <summary>Creates a successful response with no data (e.g., for DELETE or logout).</summary>
    public static ApiResponse<T> Ok(string? message = null) =>
        new() { Success = true, Message = message };

    /// <summary>Creates an error response with a message and optional error list.</summary>
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors?.ToList() ?? [] };
}

/// <summary>
/// Paginated response wrapper for list endpoints.
/// Used when returning collections that support paging (e.g., GET /api/courses).
/// </summary>
/// <typeparam name="T">The item type in the paginated list.</typeparam>
public class PagedResponse<T>
{
    /// <summary>The items for the current page.</summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>Total number of items across all pages (not just this page).</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number (1-indexed).</summary>
    public int PageNumber { get; set; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
