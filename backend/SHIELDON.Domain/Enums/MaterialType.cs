namespace SHIELDON.Domain.Enums;

/// <summary>
/// Specifies whether a course material is an uploaded file or an external link.
/// </summary>
public enum MaterialType
{
    /// <summary>An uploaded file (PDF, DOCX, PPTX, image, etc.).</summary>
    File = 0,

    /// <summary>An external URL (Google Drive, YouTube, etc.).</summary>
    Link = 1
}
