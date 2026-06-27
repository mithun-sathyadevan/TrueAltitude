using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Infrastructure.Interfaces;

public interface ILearningRepository
{
    // Read operations
    Task<List<LearningSubject>> GetSubjectsAsync();
    Task<LearningSubject?> GetSubjectByCodeAsync(string subjectCode);
    Task<LearningSubject?> GetSubjectByIdAsync(int subjectId);
    Task<int> GetTopicCountBySubjectIdAsync(int subjectId);
    Task<List<LearningTopic>> GetTopicsBySubjectIdAsync(int subjectId);
    Task<LearningTopic?> GetTopicByIdAsync(int topicId);
    Task<LearningTopic?> GetTopicByCodeAsync(string topicCode);
    Task<List<LearningTopicQuestion>> GetTopicQuestionsAsync(List<int> topicIds);
    Task<List<LearningTopicQuestion>> GetTopicQuestionsByTopicIdAsync(int topicId);
    Task<List<LearningQuestionOption>> GetOptionsByQuestionIdsAsync(List<int> questionIds);
    Task<List<LearningQuestion>> GetQuestionsByCodesWithOptionsAsync(List<string> questionCodes);
    Task<List<LearningQuestion>> GetRandomQuestionsBySubjectCodesAsync(List<string> subjectCodes, int count);
    Task<LearningQuestion?> GetQuestionByIdWithOptionsAsync(int questionId);
    Task<List<LearningQuestion>> GetAllQuestionsAsync();
    Task<int> GetTrackableTopicCountAsync();
    Task<int> GetCompletedTopicCountAsync(int userId);
    Task<List<string>> GetCompletedTopicCodesAsync(int userId);
    Task MarkTopicCompletedAsync(int userId, int topicId, DateTime completedAtUtc, int? scorePercent);
    Task<List<TopicProgressMetric>> GetTopicProgressMetricsAsync(int userId, string? subjectCode = null);

    // Create operations
    Task<LearningSubject> CreateSubjectAsync(LearningSubject subject);
    Task<LearningTopic> CreateTopicAsync(LearningTopic topic);
    Task<LearningQuestion> CreateQuestionAsync(LearningQuestion question);
    Task<LearningQuestion> CreateQuestionWithOptionsAndTopicLinkAsync(
        LearningQuestion question,
        List<LearningQuestionOption> options,
        int topicId,
        int sortOrder);
    Task<LearningQuestionOption> CreateQuestionOptionAsync(LearningQuestionOption option);
    Task<LearningTopicQuestion> CreateTopicQuestionAsync(LearningTopicQuestion topicQuestion);

    // Update operations
    Task<LearningSubject> UpdateSubjectAsync(LearningSubject subject);
    Task<LearningTopic> UpdateTopicAsync(LearningTopic topic);
    Task<LearningQuestion> UpdateQuestionAsync(LearningQuestion question);

    // Delete operations
    Task<bool> DeleteSubjectAsync(int subjectId);
    Task<bool> DeleteTopicAsync(int topicId);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<bool> DeleteQuestionOptionsAsync(int questionId);
    Task<bool> DeleteTopicQuestionAsync(int topicId, int questionId);
}

public class TopicProgressMetric
{
    public string TopicCode { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int BestPercent { get; set; }
    public int LastPercent { get; set; }
}
