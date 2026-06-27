namespace TrueAltitude.Domain.Entities;

public class UserTopicProgress
{
    public int UserId { get; set; }
    public int TopicId { get; set; }
    public bool IsCompleted { get; set; }
    public int AttemptCount { get; set; }
    public int BestPercent { get; set; }
    public int LastPercent { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }

    public User User { get; set; } = null!;
    public LearningTopic Topic { get; set; } = null!;
}
