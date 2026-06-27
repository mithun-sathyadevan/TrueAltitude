using TrueAltitude.Application.DTOs;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;

namespace TrueAltitude.Application.Services;

public interface ILearningService
{
    Task<List<LearningSubjectListItemDto>> GetSubjectsAsync();
    Task<LearningTopicNodeDto?> GetSubjectTreeAsync(string subjectCode);
    Task<bool?> TopicRequiresSubscriptionAsync(string topicCode);
    Task<List<LearningQuestionDto>?> GetTopicQuestionsAsync(string topicCode);
    Task<List<ExamQuestionDto>> GetRandomExamQuestionsAsync(List<string> subjectCodes, int count, bool hasPremiumAccess);
    Task<ExamEvaluationResultDto> EvaluateExamAnswersAsync(List<ExamQuestionRequestDto> questions, List<ExamAnswerDto> answers, int totalQuestions, bool includeExplanations);
    Task<LearningTopicProgressSummaryDto> GetTopicProgressSummaryAsync(int userId);
    Task<LearningTopicPerformanceInsightDto> GetTopicPerformanceInsightAsync(int userId, string? subjectCode = null);
    Task<List<string>> GetCompletedTopicCodesAsync(int userId);
    Task<bool> MarkTopicCompletedAsync(int userId, string topicCode, int? scorePercent);
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
        return subjects
            .OrderBy(subject => subject.RequiresSubscription)
            .ThenBy(subject => subject.SortOrder)
            .ThenBy(subject => subject.Id)
            .Select(MapSubjectSummary)
            .ToList();
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
        var questionCountByTopicId = topicQuestions
            .GroupBy(tq => tq.TopicId)
            .ToDictionary(g => g.Key, g => g.Count());

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
            QuestionCount = 0,
            RequiresSubscription = subject.RequiresSubscription,
            SubscriptionLabel = subject.SubscriptionLabel,
            Questions = new List<LearningQuestionDto>(),
            Children = BuildTopicTree(rootTopics, topicsByParentId, questionCountByTopicId)
        };

        return root;
    }

    public async Task<bool?> TopicRequiresSubscriptionAsync(string topicCode)
    {
        var topic = await _learningRepository.GetTopicByCodeAsync(topicCode);
        if (topic == null)
        {
            return null;
        }

        var subject = await _learningRepository.GetSubjectByIdAsync(topic.SubjectId);
        return topic.RequiresSubscription || (subject?.RequiresSubscription ?? false);
    }

    public async Task<List<LearningQuestionDto>?> GetTopicQuestionsAsync(string topicCode)
    {
        var topic = await _learningRepository.GetTopicByCodeAsync(topicCode);
        if (topic == null)
        {
            return null;
        }

        var topicQuestions = await _learningRepository.GetTopicQuestionsByTopicIdAsync(topic.Id);
        var questionIds = topicQuestions.Select(tq => tq.QuestionId).Distinct().ToList();
        var options = await _learningRepository.GetOptionsByQuestionIdsAsync(questionIds);

        var optionsByQuestionId = options
            .GroupBy(o => o.QuestionId)
            .ToDictionary(g => g.Key, g => g.Select(MapOption).ToList());

        return topicQuestions
            .Select(tq => MapQuestion(tq.Question, optionsByQuestionId))
            .ToList();
    }

    public async Task<List<ExamQuestionDto>> GetRandomExamQuestionsAsync(List<string> subjectCodes, int count, bool hasPremiumAccess)
    {
        var requestedCodes = new HashSet<string>(
            subjectCodes.Where(code => !string.IsNullOrWhiteSpace(code)),
            StringComparer.OrdinalIgnoreCase);

        if (requestedCodes.Count == 0)
        {
            return new List<ExamQuestionDto>();
        }

        var subjects = await _learningRepository.GetSubjectsAsync();
        var allowedSubjectCodes = subjects
            .Where(subject => requestedCodes.Contains(subject.Code))
            .Where(subject => hasPremiumAccess || !subject.RequiresSubscription)
            .Select(subject => subject.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allowedSubjectCodes.Count == 0)
        {
            return new List<ExamQuestionDto>();
        }

        var questions = await _learningRepository.GetRandomQuestionsBySubjectCodesAsync(allowedSubjectCodes, count);
        return questions
            .Select(q => new ExamQuestionDto
            {
                Id = q.Code,
                Text = q.Text,
                Options = Shuffle(q.Options
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.Id)
                    .Select(o => new ExamQuestionOptionDto
                    {
                        Id = o.Code,
                        Text = o.Text
                    }))
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
                    Explanation = string.Empty,
                    AnswerImageUrl = null
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
                Explanation = includeExplanations ? (correctOption?.Explanation ?? string.Empty) : string.Empty,
                AnswerImageUrl = question.AnswerImageUrl
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

    public async Task<LearningTopicProgressSummaryDto> GetTopicProgressSummaryAsync(int userId)
    {
        var totalTopics = await _learningRepository.GetTrackableTopicCountAsync();
        var coveredTopics = await _learningRepository.GetCompletedTopicCountAsync(userId);

        var safeCoveredTopics = Math.Min(Math.Max(coveredTopics, 0), Math.Max(totalTopics, 0));
        var remainingTopics = Math.Max(totalTopics - safeCoveredTopics, 0);
        var coveragePercent = totalTopics == 0
            ? 0
            : (int)Math.Round((double)safeCoveredTopics * 100 / totalTopics);

        return new LearningTopicProgressSummaryDto
        {
            TotalTopics = totalTopics,
            CoveredTopics = safeCoveredTopics,
            RemainingTopics = remainingTopics,
            CoveragePercent = coveragePercent,
        };
    }

    public async Task<LearningTopicPerformanceInsightDto> GetTopicPerformanceInsightAsync(int userId, string? subjectCode = null)
    {
        var metrics = await _learningRepository.GetTopicProgressMetricsAsync(userId, subjectCode);
        if (metrics.Count == 0)
        {
            return new LearningTopicPerformanceInsightDto
            {
                Recommendation = "Finish at least one topic quiz to unlock score-based insights.",
            };
        }

        var scoredMetrics = metrics
            .Where(metric => metric.AttemptCount > 0)
            .ToList();

        var attemptedTopics = metrics.Count;
        var averageScorePercent = scoredMetrics.Count == 0
            ? 0
            : (int)Math.Round(scoredMetrics.Average(metric => metric.LastPercent));
        var consistencyPercent = scoredMetrics.Count == 0
            ? 0
            : (int)Math.Round(100.0 * scoredMetrics.Count(metric => metric.LastPercent >= 70) / scoredMetrics.Count);

        var strongMetrics = scoredMetrics
            .Where(metric => metric.BestPercent >= 70)
            .OrderByDescending(metric => metric.BestPercent)
            .ThenByDescending(metric => metric.AttemptCount)
            .Take(3)
            .ToList();

        var strongTopicCodes = new HashSet<string>(
            strongMetrics.Select(metric => metric.TopicCode),
            StringComparer.OrdinalIgnoreCase);

        var improvementCandidates = metrics
            .Where(metric => !strongTopicCodes.Contains(metric.TopicCode))
            .Where(metric => metric.AttemptCount == 0 || metric.LastPercent < 70)
            .OrderBy(metric => metric.LastPercent)
            .ThenBy(metric => metric.AttemptCount)
            .ThenBy(metric => metric.BestPercent)
            .Take(3)
            .ToList();

        if (improvementCandidates.Count == 0)
        {
            improvementCandidates = metrics
                .Where(metric => !strongTopicCodes.Contains(metric.TopicCode))
                .OrderBy(metric => metric.LastPercent)
                .ThenBy(metric => metric.AttemptCount)
                .ThenBy(metric => metric.BestPercent)
                .Take(3)
                .ToList();
        }

        var improvementTopics = improvementCandidates
            .Select(MapTopicScoreInsight)
            .ToList();

        var strongTopics = strongMetrics
            .Select(MapTopicScoreInsight)
            .ToList();

        return new LearningTopicPerformanceInsightDto
        {
            AttemptedTopics = attemptedTopics,
            AverageScorePercent = averageScorePercent,
            ConsistencyPercent = consistencyPercent,
            Recommendation = scoredMetrics.Count == 0
                ? "Completed topics found, but no quiz score captured yet. Finish quizzes to unlock score trends."
                : BuildRecommendation(averageScorePercent, consistencyPercent, attemptedTopics),
            StrongTopics = strongTopics,
            ImprovementTopics = improvementTopics,
        };
    }

    public async Task<List<string>> GetCompletedTopicCodesAsync(int userId)
    {
        return await _learningRepository.GetCompletedTopicCodesAsync(userId);
    }

    public async Task<bool> MarkTopicCompletedAsync(int userId, string topicCode, int? scorePercent)
    {
        var normalizedTopicCode = (topicCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedTopicCode))
        {
            return false;
        }

        var topic = await _learningRepository.GetTopicByCodeAsync(normalizedTopicCode);
        if (topic == null)
        {
            return false;
        }

        await _learningRepository.MarkTopicCompletedAsync(userId, topic.Id, DateTime.UtcNow, scorePercent);
        return true;
    }

    private static LearningTopicScoreInsightDto MapTopicScoreInsight(TopicProgressMetric metric)
    {
        return new LearningTopicScoreInsightDto
        {
            TopicCode = metric.TopicCode,
            TopicTitle = metric.TopicTitle,
            AttemptCount = metric.AttemptCount,
            BestPercent = metric.BestPercent,
            LastPercent = metric.LastPercent,
        };
    }

    private static string BuildRecommendation(int averageScorePercent, int consistencyPercent, int attemptedTopics)
    {
        if (attemptedTopics < 3)
        {
            return "Attempt a few more topics to make this insight more reliable.";
        }

        if (averageScorePercent >= 80 && consistencyPercent >= 70)
        {
            return "Strong momentum. Keep revision cycles short and move to timed exam practice.";
        }

        if (averageScorePercent >= 65)
        {
            return "Good baseline. Focus on your lowest-score topics before your next timed exam.";
        }

        return "Prioritize concept revision in weak topics, then retake those quizzes for faster improvement.";
    }

    private static List<LearningTopicNodeDto> BuildTopicTree(
        IEnumerable<LearningTopic> topics,
        Dictionary<int, List<LearningTopic>> topicsByParentId,
        Dictionary<int, int> questionCountByTopicId)
    {
        return topics
            .Select(topic => new LearningTopicNodeDto
            {
                Id = topic.Code,
                Title = topic.Title,
                Description = topic.Description,
                QuestionCount = questionCountByTopicId.TryGetValue(topic.Id, out var count)
                    ? count
                    : 0,
                RequiresSubscription = topic.RequiresSubscription,
                SubscriptionLabel = topic.SubscriptionLabel,
                Questions = new List<LearningQuestionDto>(),
                Children = topicsByParentId.TryGetValue(topic.Id, out var children)
                    ? BuildTopicTree(children, topicsByParentId, questionCountByTopicId)
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
            AnswerImageUrl = question.AnswerImageUrl,
            Explanation = question.ExplanationText ?? string.Empty,
            RequiresSubscription = question.RequiresSubscription,
            SubscriptionLabel = question.SubscriptionLabel,
            Options = optionsByQuestionId.TryGetValue(question.Id, out var options)
                ? Shuffle(options)
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
            Explanation = option.Explanation ?? string.Empty
        };
    }

    private static List<T> Shuffle<T>(IEnumerable<T> source)
    {
        var list = source.ToList();

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
