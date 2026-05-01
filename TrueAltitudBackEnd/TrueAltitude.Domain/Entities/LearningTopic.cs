namespace TrueAltitude.Domain.Entities;

public class LearningTopic
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? ParentTopicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public int SortOrder { get; set; }

    public LearningSubject Subject { get; set; } = null!;
    public LearningTopic? ParentTopic { get; set; }
    public ICollection<LearningTopic> Children { get; set; } = new List<LearningTopic>();
    public ICollection<LearningTopicQuestion> TopicQuestions { get; set; } = new List<LearningTopicQuestion>();
}
