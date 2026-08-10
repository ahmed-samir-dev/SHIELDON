using Ganss.Xss;

namespace SHIELDON.Infrastructure.Common;

/// <summary>
/// Provides text sanitization to protect against XSS vulnerabilities across stored user inputs.
/// </summary>
public static class SanitizationHelper
{
    private static readonly HtmlSanitizer Sanitizer = new();

    static SanitizationHelper()
    {
        // Strip all HTML tags - plain text only
        Sanitizer.AllowedTags.Clear();
        Sanitizer.AllowedAttributes.Clear();
        Sanitizer.AllowedCssProperties.Clear();
        Sanitizer.AllowedSchemes.Clear();
    }

    /// <summary>
    /// Strips all HTML tags and attributes from user input, returning safe plain text.
    /// Returns empty string if input is null or whitespace.
    /// </summary>
    public static string StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return Sanitizer.Sanitize(input).Trim();
    }
}
