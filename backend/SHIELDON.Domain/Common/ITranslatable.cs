namespace SHIELDON.Domain.Common;

/// <summary>
/// Indicates that an entity has fields that can be automatically translated and stored as JSON.
/// </summary>
public interface ITranslatable
{
    /// <summary>
    /// Stores the translations in JSON format. 
    /// Example: {"ar": {"Title": "الرياضة", "Description": "..."}}
    /// </summary>
    string? Translations { get; set; }
}
