using TrueAltitude.Domain.Entities;

namespace TrueAltitude.Infrastructure.Interfaces;

public interface ILearningRepository
{
    Task<List<LearningSubject>> GetSubjectsAsync();
    Task<LearningSubject?> GetSubjectByCodeAsync(string subjectCode);
    Task<List<LearningTopic>> GetTopicsBySubjectIdAsync(int subjectId);
    Task<List<LearningTopicQuestion>> GetTopicQuestionsAsync(List<int> topicIds);
    Task<List<LearningQuestionOption>> GetOptionsByQuestionIdsAsync(List<int> questionIds);
    Task<List<LearningQuestion>> GetQuestionsByCodesWithOptionsAsync(List<string> questionCodes);
    Task<List<LearningQuestion>> GetRandomQuestionsBySubjectCodesAsync(List<string> subjectCodes, int count);
}
