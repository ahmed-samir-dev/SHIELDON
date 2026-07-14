namespace SHIELDON.Domain.Enums;

/// <summary>
/// Categorises the type of file attachment on a chat message.
/// Controls rendering logic on the frontend (audio player, image preview, or document download).
/// </summary>
public enum AttachmentType
{
    /// <summary>No attachment — plain text message.</summary>
    None = 0,

    /// <summary>Voice note recorded via MediaRecorder API (max 5 minutes).</summary>
    Audio = 1,

    /// <summary>Image file (JPG, PNG).</summary>
    Image = 2,

    /// <summary>Document file (PDF, DOCX).</summary>
    Document = 3
}
