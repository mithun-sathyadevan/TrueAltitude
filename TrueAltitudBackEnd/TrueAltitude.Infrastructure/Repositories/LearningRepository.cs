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

        // If not enough questions from selected subjects, fetch more from all subjects
        if (selectedQuestionIds.Count < count)
        {
            Console.WriteLine($"[GetRandomQuestions] Not enough questions ({selectedQuestionIds.Count} < {count}), fetching more from all subjects");
            
            var allQuestionIds = await _context.LearningQuestions
                .AsNoTracking()
                .Where(q => !selectedQuestionIds.Contains(q.Id))
                .Select(q => q.Id)
                .ToListAsync();

            var additionalCount = count - selectedQuestionIds.Count;
            var additionalIds = allQuestionIds.OrderBy(_ => random.Next()).Take(additionalCount).ToList();
            selectedQuestionIds.AddRange(additionalIds);

            Console.WriteLine($"[GetRandomQuestions] Added {additionalIds.Count} additional questions, total now {selectedQuestionIds.Count}");
        }

        // Fetch the full questions with options
        var questions = await _context.LearningQuestions
            .AsNoTracking()
            .Include(q => q.Options)
            .Where(q => selectedQuestionIds.Contains(q.Id))
            .ToListAsync();

        Console.WriteLine($"[GetRandomQuestions] Returning {questions.Count} full questions with options");

        return questions;
    }
}
