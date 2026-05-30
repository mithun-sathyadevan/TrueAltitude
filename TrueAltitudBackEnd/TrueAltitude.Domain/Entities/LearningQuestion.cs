namespace TrueAltitude.Domain.Entities;

public class LearningQuestion
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? AnswerImageUrl { get; set; }
    public string Type { get; set; } = "multiple_choice"; // multiple_choice, true_false, short_answer
    public string? ExplanationText { get; set; }
    public int Difficulty { get; set; } = 1; // 1-5
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public int SortOrder { get; set; }

    public ICollection<LearningTopicQuestion> TopicQuestions { get; set; } = new List<LearningTopicQuestion>();
    public ICollection<LearningQuestionOption> Options { get; set; } = new List<LearningQuestionOption>();
}
