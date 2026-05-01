using TrueAltitude.Application.DTOs;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;

namespace TrueAltitude.Application.Services;

public interface ILearningService
{
    Task<List<LearningSubjectListItemDto>> GetSubjectsAsync();
    Task<LearningTopicNodeDto?> GetSubjectTreeAsync(string subjectCode);
    Task<List<ExamQuestionDto>> GetRandomExamQuestionsAsync(List<string> subjectCodes, int count);
    Task<ExamEvaluationResultDto> EvaluateExamAnswersAsync(List<ExamQuestionRequestDto> questions, List<ExamAnswerDto> answers, int totalQuestions, bool includeExplanations);
}

public class LearningService : ILearningService
{
    private readonly ILearningRepository _learningRepository;

    public LearningService(ILearningRepository learningRepository)
    {
        _learningRepository = learningRepository;
    }

    public async Task<List<LearningSubjectListItemDto>> GetSubjectsAsync()
    {
        var subjects = await _learningRepository.GetSubjectsAsync();
        return subjects.Select(MapSubjectSummary).ToList();
    }

    public async Task<LearningTopicNodeDto?> GetSubjectTreeAsync(string subjectCode)
    {
        var subject = await _learningRepository.GetSubjectByCodeAsync(subjectCode);
        if (subject == null)
        {
            return null;
        }

        var topics = await _learningRepository.GetTopicsBySubjectIdAsync(subject.Id);
        var topicIds = topics.Select(t => t.Id).ToList();
        var topicQuestions = await _learningRepository.GetTopicQuestionsAsync(topicIds);
        var questionIds = topicQuestions.Select(tq => tq.QuestionId).Distinct().ToList();
        var options = await _learningRepository.GetOptionsByQuestionIdsAsync(questionIds);

        var optionsByQuestionId = options
            .GroupBy(o => o.QuestionId)
            .ToDictionary(g => g.Key, g => g.Select(MapOption).ToList());

        var questionsByTopicId = topicQuestions
            .GroupBy(tq => tq.TopicId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(tq => MapQuestion(tq.Question, optionsByQuestionId)).ToList());

        var rootTopics = topics
            .Where(t => !t.ParentTopicId.HasValue)
            .ToList();

        var topicsByParentId = topics
            .Where(t => t.ParentTopicId.HasValue)
            .GroupBy(t => t.ParentTopicId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var root = new LearningTopicNodeDto
        {
            Id = subject.Code,
            Title = subject.Title,
            Description = subject.Description,
            RequiresSubscription = subject.RequiresSubscription,
            SubscriptionLabel = subject.SubscriptionLabel,
            Questions = new List<LearningQuestionDto>(),
            Children = BuildTopicTree(rootTopics, topicsByParentId, questionsByTopicId)
        };

        return root;
    }

    public async Task<List<ExamQuestionDto>> GetRandomExamQuestionsAsync(List<string> subjectCodes, int count)
    {
        var questions = await _learningRepository.GetRandomQuestionsBySubjectCodesAsync(subjectCodes, count);
        return questions
            .Select(q => new ExamQuestionDto
            {
                Id = q.Code,
                Text = q.Text,
                Options = q.Options
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Id)
                    .Select(o => new ExamQuestionOptionDto
                    {
                        Id = o.Code,
                        Text = o.Text
                    })
                    .ToList()
            })
            .ToList();
    }

    public async Task<ExamEvaluationResultDto> EvaluateExamAnswersAsync(List<ExamQuestionRequestDto> requestedQuestions, List<ExamAnswerDto> answers, int totalQuestions, bool includeExplanations)
    {
        if (requestedQuestions.Count == 0)
        {
            return new ExamEvaluationResultDto
            {
                Score = 0,
                TotalQuestions = totalQuestions > 0 ? totalQuestions : 0,
                Percent = 0,
                Results = new List<ExamQuestionEvaluationDto>()
            };
        }

        var answerByQuestionId = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.QuestionId))
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().OptionId);

        var questionCodes = requestedQuestions
            .Select(q => q.QuestionId)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .Distinct()
            .ToList();

        var questionEntities = await _learningRepository.GetQuestionsByCodesWithOptionsAsync(questionCodes);
        var questionsByCode = questionEntities.ToDictionary(q => q.Code, q => q);

        var results = questionCodes.Select(questionCode =>
        {
            if (!questionsByCode.TryGetValue(questionCode, out var question))
            {
                return new ExamQuestionEvaluationDto
                {
                    QuestionId = questionCode,
                    IsCorrect = false,
                    IsAnswered = false,
                    CorrectOptionId = string.Empty,
                    CorrectOptionText = string.Empty,
                    Explanation = string.Empty
                };
            }

            var correctOption = question.Options
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Id)
                .FirstOrDefault(o => o.IsCorrect);

            answerByQuestionId.TryGetValue(questionCode, out var selectedOptionCode);
            var selectedOption = question.Options.FirstOrDefault(o => o.Code == selectedOptionCode);
            var isAnswered = !string.IsNullOrWhiteSpace(selectedOptionCode);
            var isCorrect = selectedOption?.IsCorrect ?? false;

            return new ExamQuestionEvaluationDto
            {
                QuestionId = questionCode,
                IsCorrect = isCorrect,
                IsAnswered = isAnswered,
                CorrectOptionId = correctOption?.Code ?? string.Empty,
                CorrectOptionText = correctOption?.Text ?? string.Empty,
                Explanation = includeExplanations ? (correctOption?.Explanation ?? string.Empty) : string.Empty
            };
        }).ToList();

        var score = results.Count(r => r.IsCorrect);
        var effectiveTotal = totalQuestions > 0 ? totalQuestions : questionCodes.Count;
        var percent = effectiveTotal == 0 ? 0 : (int)Math.Round((double)score * 100 / effectiveTotal);

        return new ExamEvaluationResultDto
        {
            Score = score,
            TotalQuestions = effectiveTotal,
            Percent = percent,
            Results = results
        };
    }

    private static List<LearningTopicNodeDto> BuildTopicTree(
        IEnumerable<LearningTopic> topics,
        Dictionary<int, List<LearningTopic>> topicsByParentId,
        Dictionary<int, List<LearningQuestionDto>> questionsByTopicId)
    {
        return topics
            .Select(topic => new LearningTopicNodeDto
            {
                Id = topic.Code,
                Title = topic.Title,
                Description = topic.Description,
                RequiresSubscription = topic.RequiresSubscription,
                SubscriptionLabel = topic.SubscriptionLabel,
                Questions = questionsByTopicId.TryGetValue(topic.Id, out var questions)
                    ? questions
                    : new List<LearningQuestionDto>(),
                Children = topicsByParentId.TryGetValue(topic.Id, out var children)
                    ? BuildTopicTree(children, topicsByParentId, questionsByTopicId)
                    : new List<LearningTopicNodeDto>()
            })
            .ToList();
    }

    private static LearningSubjectListItemDto MapSubjectSummary(LearningSubject subject)
    {
        return new LearningSubjectListItemDto
        {
            Id = subject.Code,
            Title = subject.Title,
            Description = subject.Description,
            RequiresSubscription = subject.RequiresSubscription,
            SubscriptionLabel = subject.SubscriptionLabel
        };
    }

    private static LearningQuestionDto MapQuestion(
        LearningQuestion question,
        Dictionary<int, List<LearningQuestionOptionDto>> optionsByQuestionId)
    {
        return new LearningQuestionDto
        {
            Id = question.Code,
            Text = question.Text,
            RequiresSubscription = question.RequiresSubscription,
            SubscriptionLabel = question.SubscriptionLabel,
            Options = optionsByQuestionId.TryGetValue(question.Id, out var options)
                ? options
                : new List<LearningQuestionOptionDto>()
        };
    }

    private static LearningQuestionOptionDto MapOption(LearningQuestionOption option)
    {
        return new LearningQuestionOptionDto
        {
            Id = option.Code,
            Text = option.Text,
            IsCorrect = option.IsCorrect,
            Explanation = option.Explanation
        };
    }
}
