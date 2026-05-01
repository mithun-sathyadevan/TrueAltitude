namespace TrueAltitude.Domain.Entities;

/// <summary>
/// Junction table linking a master LearningQuestion to one or more LearningTopics.
/// A question can appear in multiple topics; each link has its own SortOrder.
/// </summary>
public class LearningTopicQuestion
{
    public int TopicId { get; set; }
    public int QuestionId { get; set; }
    public int SortOrder { get; set; }

    public LearningTopic Topic { get; set; } = null!;
    public LearningQuestion Question { get; set; } = null!;
}
