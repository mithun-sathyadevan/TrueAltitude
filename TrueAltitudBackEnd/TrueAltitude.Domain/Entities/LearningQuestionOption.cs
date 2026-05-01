namespace TrueAltitude.Domain.Entities;

public class LearningQuestionOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public LearningQuestion Question { get; set; } = null!;
}
