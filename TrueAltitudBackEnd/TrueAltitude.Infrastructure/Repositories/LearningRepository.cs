using Microsoft.EntityFrameworkCore;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;
using TrueAltitude.Persistence.Data;

namespace TrueAltitude.Infrastructure.Repositories;

public class LearningRepository : ILearningRepository
{
    private readonly TrueAltitudeDbContext _context;

    public LearningRepository(TrueAltitudeDbContext context)
    {
        _context = context;
    }

    public async Task<List<LearningSubject>> GetSubjectsAsync()
    {
        return await _context.LearningSubjects
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Title)
            .ToListAsync();
    }

    public async Task<LearningSubject?> GetSubjectByCodeAsync(string subjectCode)
    {
        return await _context.LearningSubjects
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == subjectCode);
    }

    public async Task<List<LearningTopic>> GetTopicsBySubjectIdAsync(int subjectId)
    {
        return await _context.LearningTopics
            .AsNoTracking()
            .Include(t => t.ParentTopic)
            .Include(t => t.TopicQuestions)
            .Where(t => t.SubjectId == subjectId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .ToListAsync();
    }

    public async Task<List<LearningTopicQuestion>> GetTopicQuestionsAsync(List<int> topicIds)
    {
        if (topicIds.Count == 0)
        {
            return new List<LearningTopicQuestion>();
        }

        return await _context.LearningTopicQuestions
            .AsNoTracking()
            .Include(tq => tq.Question)
            .Where(tq => topicIds.Contains(tq.TopicId))
            .OrderBy(tq => tq.SortOrder)
            .ThenBy(tq => tq.QuestionId)
            .ToListAsync();
    }

    public async Task<List<LearningQuestionOption>> GetOptionsByQuestionIdsAsync(List<int> questionIds)
    {
        if (questionIds.Count == 0)
        {
            return new List<LearningQuestionOption>();
        }

        return await _context.LearningQuestionOptions
            .AsNoTracking()
            .Where(o => questionIds.Contains(o.QuestionId))
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.Id)
            .ToListAsync();
    }

    public async Task<List<LearningQuestion>> GetQuestionsByCodesWithOptionsAsync(List<string> questionCodes)
    {
        if (questionCodes.Count == 0)
        {
            return new List<LearningQuestion>();
        }

        return await _context.LearningQuestions
            .AsNoTracking()
            .Include(q => q.Options)
            .Where(q => questionCodes.Contains(q.Code))
            .ToListAsync();
    }

    public async Task<List<LearningQuestion>> GetRandomQuestionsBySubjectCodesAsync(List<string> subjectCodes, int count)
    {
        if (subjectCodes.Count == 0)
        {
            return new List<LearningQuestion>();
        }

        Console.WriteLine($"[GetRandomQuestions] Requested subject codes: {string.Join(", ", subjectCodes)}");

        // Get all subjects matching the codes
        var subjects = await _context.LearningSubjects
            .AsNoTracking()
            .Where(s => subjectCodes.Contains(s.Code))
            .ToListAsync();

        Console.WriteLine($"[GetRandomQuestions] Found {subjects.Count} subjects");

        if (subjects.Count == 0)
        {
            return new List<LearningQuestion>();
        }

        var subjectIds = subjects.Select(s => s.Id).ToList();

        // Get all topics for the selected subjects
        var topics = await _context.LearningTopics
            .AsNoTracking()
            .Where(t => subjectIds.Contains(t.SubjectId))
            .ToListAsync();

        Console.WriteLine($"[GetRandomQuestions] Found {topics.Count} topics for subjects");

        var topicIds = topics.Select(t => t.Id).ToList();

        // Get all questions linked to these topics
        var questionIds = await _context.LearningTopicQuestions
            .AsNoTracking()
            .Where(tq => topicIds.Contains(tq.TopicId))
            .Select(tq => tq.QuestionId)
            .Distinct()
            .ToListAsync();

        Console.WriteLine($"[GetRandomQuestions] Found {questionIds.Count} distinct questions in LearningTopicQuestions junction");

        var random = new Random();
        var selectedQuestionIds = questionIds.OrderBy(_ => random.Next()).Take(count).ToList();

        Console.WriteLine($"[GetRandomQuestions] Selected {selectedQuestionIds.Count} random questions");

        // Fetch the full questions with options
        var questions = await _context.LearningQuestions
            .AsNoTracking()
            .Include(q => q.Options)
            .Where(q => selectedQuestionIds.Contains(q.Id))
            .ToListAsync();

        Console.WriteLine($"[GetRandomQuestions] Returning {questions.Count} full questions with options");

        return questions;
    }

    // Additional methods for admin operations
    public async Task<LearningSubject?> GetSubjectByIdAsync(int subjectId)
    {
        return await _context.LearningSubjects
            .FirstOrDefaultAsync(s => s.Id == subjectId);
    }

    public async Task<int> GetTopicCountBySubjectIdAsync(int subjectId)
    {
        return await _context.LearningTopics
            .CountAsync(t => t.SubjectId == subjectId);
    }

    public async Task<LearningTopic?> GetTopicByIdAsync(int topicId)
    {
        return await _context.LearningTopics
            .AsNoTracking()
            .Include(t => t.ParentTopic)
            .Include(t => t.TopicQuestions)
            .FirstOrDefaultAsync(t => t.Id == topicId);
    }

    public async Task<LearningTopic?> GetTopicByCodeAsync(string topicCode)
    {
        var normalizedTopicCode = (topicCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedTopicCode))
        {
            return null;
        }

        var loweredTopicCode = normalizedTopicCode.ToLower();

        return await _context.LearningTopics
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code.ToLower() == loweredTopicCode);
    }

    public async Task<List<LearningTopicQuestion>> GetTopicQuestionsByTopicIdAsync(int topicId)
    {
        return await _context.LearningTopicQuestions
            .AsNoTracking()
            .Include(tq => tq.Question)
            .Where(tq => tq.TopicId == topicId)
            .OrderBy(tq => tq.SortOrder)
            .ThenBy(tq => tq.QuestionId)
            .ToListAsync();
    }

    public async Task<LearningQuestion?> GetQuestionByIdWithOptionsAsync(int questionId)
    {
        return await _context.LearningQuestions
            .AsNoTracking()
            .Include(q => q.Options)
            .Include(q => q.TopicQuestions)
                .ThenInclude(tq => tq.Topic)
                    .ThenInclude(t => t.ParentTopic)
            .FirstOrDefaultAsync(q => q.Id == questionId);
    }

    public async Task<List<LearningQuestion>> GetAllQuestionsAsync()
    {
        return await _context.LearningQuestions
            .AsNoTracking()
            .Include(q => q.Options)
            .Include(q => q.TopicQuestions)
                .ThenInclude(tq => tq.Topic)
                    .ThenInclude(t => t.ParentTopic)
            .ToListAsync();
    }

    public async Task<int> GetTrackableTopicCountAsync()
    {
        return await _context.LearningTopics
            .AsNoTracking()
            .Where(topic => topic.TopicQuestions.Any())
            .CountAsync();
    }

    public async Task<int> GetCompletedTopicCountAsync(int userId)
    {
        return await _context.UserTopicProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.IsCompleted)
            .Where(progress => _context.LearningTopicQuestions.Any(tq => tq.TopicId == progress.TopicId))
            .CountAsync();
    }

    public async Task<List<string>> GetCompletedTopicCodesAsync(int userId)
    {
        return await _context.UserTopicProgresses
            .AsNoTracking()
            .Where(progress => progress.UserId == userId && progress.IsCompleted)
            .Where(progress => _context.LearningTopicQuestions.Any(tq => tq.TopicId == progress.TopicId))
            .Join(
                _context.LearningTopics.AsNoTracking(),
                progress => progress.TopicId,
                topic => topic.Id,
                (_, topic) => topic.Code)
            .Distinct()
            .ToListAsync();
    }

    public async Task MarkTopicCompletedAsync(int userId, int topicId, DateTime completedAtUtc, int? scorePercent)
    {
        var topicHasQuestions = await _context.LearningTopicQuestions
            .AsNoTracking()
            .AnyAsync(topicQuestion => topicQuestion.TopicId == topicId);

        if (!topicHasQuestions)
        {
            return;
        }

        var existing = await _context.UserTopicProgresses
            .FirstOrDefaultAsync(progress => progress.UserId == userId && progress.TopicId == topicId);

        var safeScorePercent = scorePercent.HasValue
            ? Math.Clamp(scorePercent.Value, 0, 100)
            : (int?)null;

        if (existing == null)
        {
            await _context.UserTopicProgresses.AddAsync(new UserTopicProgress
            {
                UserId = userId,
                TopicId = topicId,
                IsCompleted = true,
                CompletedAt = completedAtUtc,
                LastAttemptAt = safeScorePercent.HasValue ? completedAtUtc : null,
                AttemptCount = safeScorePercent.HasValue ? 1 : 0,
                LastPercent = safeScorePercent ?? 0,
                BestPercent = safeScorePercent ?? 0,
            });
        }
        else
        {
            existing.IsCompleted = true;
            existing.CompletedAt = completedAtUtc;

            if (safeScorePercent.HasValue)
            {
                existing.AttemptCount = Math.Max(existing.AttemptCount, 0) + 1;
                existing.LastPercent = safeScorePercent.Value;
                existing.BestPercent = Math.Max(existing.BestPercent, safeScorePercent.Value);
                existing.LastAttemptAt = completedAtUtc;
            }

            _context.UserTopicProgresses.Update(existing);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<TopicProgressMetric>> GetTopicProgressMetricsAsync(int userId, string? subjectCode = null)
    {
        var normalizedSubjectCode = (subjectCode ?? string.Empty).Trim();
        var hasSubjectFilter = !string.IsNullOrWhiteSpace(normalizedSubjectCode);

        var query =
            from progress in _context.UserTopicProgresses.AsNoTracking()
            join topic in _context.LearningTopics.AsNoTracking() on progress.TopicId equals topic.Id
            join subject in _context.LearningSubjects.AsNoTracking() on topic.SubjectId equals subject.Id
            where progress.UserId == userId
                && progress.IsCompleted
                && _context.LearningTopicQuestions.Any(tq => tq.TopicId == progress.TopicId)
                && (!hasSubjectFilter || subject.Code == normalizedSubjectCode)
            select new TopicProgressMetric
            {
                TopicCode = topic.Code,
                TopicTitle = topic.Title,
                AttemptCount = progress.AttemptCount,
                BestPercent = progress.BestPercent,
                LastPercent = progress.LastPercent,
            };

        return await query.ToListAsync();
    }

    // Create operations
    public async Task<LearningSubject> CreateSubjectAsync(LearningSubject subject)
    {
        _context.LearningSubjects.Add(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task<LearningTopic> CreateTopicAsync(LearningTopic topic)
    {
        _context.LearningTopics.Add(topic);
        await _context.SaveChangesAsync();
        return topic;
    }

    public async Task<LearningQuestion> CreateQuestionAsync(LearningQuestion question)
    {
        _context.LearningQuestions.Add(question);
        await _context.SaveChangesAsync();
        return question;
    }

    public async Task<LearningQuestion> CreateQuestionWithOptionsAndTopicLinkAsync(
        LearningQuestion question,
        List<LearningQuestionOption> options,
        int topicId,
        int sortOrder)
    {
        foreach (var option in options)
        {
            option.Question = question;
        }

        var topicQuestion = new LearningTopicQuestion
        {
            TopicId = topicId,
            Question = question,
            SortOrder = sortOrder
        };

        _context.LearningQuestions.Add(question);
        _context.LearningQuestionOptions.AddRange(options);
        _context.LearningTopicQuestions.Add(topicQuestion);

        await _context.SaveChangesAsync();
        return question;
    }

    public async Task<LearningQuestionOption> CreateQuestionOptionAsync(LearningQuestionOption option)
    {
        _context.LearningQuestionOptions.Add(option);
        await _context.SaveChangesAsync();
        return option;
    }

    public async Task<LearningTopicQuestion> CreateTopicQuestionAsync(LearningTopicQuestion topicQuestion)
    {
        _context.LearningTopicQuestions.Add(topicQuestion);
        await _context.SaveChangesAsync();
        return topicQuestion;
    }

    // Update operations
    public async Task<LearningSubject> UpdateSubjectAsync(LearningSubject subject)
    {
        _context.LearningSubjects.Update(subject);
        await _context.SaveChangesAsync();
        return subject;
    }

    public async Task<LearningTopic> UpdateTopicAsync(LearningTopic topic)
    {
        _context.LearningTopics.Update(topic);
        await _context.SaveChangesAsync();
        return topic;
    }

    public async Task<LearningQuestion> UpdateQuestionAsync(LearningQuestion question)
    {
        _context.LearningQuestions.Update(question);
        await _context.SaveChangesAsync();
        return question;
    }

    // Delete operations
    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        var subject = await _context.LearningSubjects.FindAsync(subjectId);
        if (subject == null)
            return false;

        _context.LearningSubjects.Remove(subject);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTopicAsync(int topicId)
    {
        var topic = await _context.LearningTopics.FindAsync(topicId);
        if (topic == null)
            return false;

        _context.LearningTopics.Remove(topic);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        var question = await _context.LearningQuestions.FindAsync(questionId);
        if (question == null)
            return false;

        _context.LearningQuestions.Remove(question);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuestionOptionsAsync(int questionId)
    {
        var options = await _context.LearningQuestionOptions
            .Where(o => o.QuestionId == questionId)
            .ToListAsync();

        if (options.Count == 0)
            return true;

        _context.LearningQuestionOptions.RemoveRange(options);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTopicQuestionAsync(int topicId, int questionId)
    {
        var link = await _context.LearningTopicQuestions
            .FirstOrDefaultAsync(tq => tq.TopicId == topicId && tq.QuestionId == questionId);

        if (link == null)
            return false;

        _context.LearningTopicQuestions.Remove(link);
        await _context.SaveChangesAsync();
        return true;
    }
}
