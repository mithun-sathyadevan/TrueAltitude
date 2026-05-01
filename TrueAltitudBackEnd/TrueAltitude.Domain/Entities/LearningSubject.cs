namespace TrueAltitude.Domain.Entities;

public class LearningSubject
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public int SortOrder { get; set; }

    public ICollection<LearningTopic> Topics { get; set; } = new List<LearningTopic>();
}
