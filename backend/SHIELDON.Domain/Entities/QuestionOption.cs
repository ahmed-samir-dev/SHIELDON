using SHIELDON.Domain.Common;

namespace SHIELDON.Domain.Entities;

public class QuestionOption : ITranslatable
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    [Translatable]
    public string OptionText { get; set; } = string.Empty;
    
    public string? Translations { get; set; }
    public bool IsCorrect { get; set; }

    public ExamQuestion? Question { get; set; }
}
