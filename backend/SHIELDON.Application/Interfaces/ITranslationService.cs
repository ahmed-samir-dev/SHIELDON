namespace SHIELDON.Application.Interfaces;

public interface ITranslationService
{
    /// <summary>
    /// Translates the given text from the source language to the target language.
    /// </summary>
    Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "en", CancellationToken cancellationToken = default);
}
