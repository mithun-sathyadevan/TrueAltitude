namespace TrueAltitude.Domain.Entities;

public class LearningQuestion
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public int SortOrder { get; set; }

    public ICollection<LearningTopicQuestion> TopicQuestions { get; set; } = new List<LearningTopicQuestion>();
    public ICollection<LearningQuestionOption> Options { get; set; } = new List<LearningQuestionOption>();
}
