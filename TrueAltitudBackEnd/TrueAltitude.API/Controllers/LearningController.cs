using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Application.Services;

namespace TrueAltitude.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LearningController : ControllerBase
{
    private readonly ILearningService _learningService;
    private readonly IConfiguration _configuration;

    public LearningController(ILearningService learningService, IConfiguration configuration)
    {
        _learningService = learningService;
        _configuration = configuration;
    }

    [HttpGet("exam/config")]
    public IActionResult GetExamConfig()
    {
        var (questionCount, durationMinutes) = GetConfiguredExamSettings();
        return Ok(new ExamConfigResponse
        {
            QuestionCount = questionCount,
            DurationMinutes = durationMinutes,
        });
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var subjects = await _learningService.GetSubjectsAsync();
        return Ok(subjects);
    }

    [HttpGet("progress/summary")]
    public async Task<IActionResult> GetTopicProgressSummary()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var summary = await _learningService.GetTopicProgressSummaryAsync(userId.Value);
        return Ok(summary);
    }

    [HttpGet("progress/insights")]
    public async Task<IActionResult> GetTopicPerformanceInsights([FromQuery] string? subjectCode)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var insights = await _learningService.GetTopicPerformanceInsightAsync(userId.Value, subjectCode);
        return Ok(insights);
    }

    [HttpGet("progress/topics/completed")]
    public async Task<IActionResult> GetCompletedTopicCodes()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var topicCodes = await _learningService.GetCompletedTopicCodesAsync(userId.Value);
        return Ok(topicCodes);
    }

    [HttpPost("progress/topics/{topicCode}/complete")]
    public async Task<IActionResult> MarkTopicCompleted(string topicCode)
    {
        return await MarkTopicCompletedInternal(topicCode, null);
    }

    [HttpPost("progress/topics/complete")]
    public async Task<IActionResult> MarkTopicCompletedByBody([FromBody] TopicCompletionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TopicCode))
        {
            return BadRequest(new { message = "Topic code is required." });
        }

        return await MarkTopicCompletedInternal(request.TopicCode, request.ScorePercent);
    }

    [HttpGet("subjects/{subjectCode}")]
    public async Task<IActionResult> GetSubjectByCode(string subjectCode)
    {
        var subject = await _learningService.GetSubjectTreeAsync(subjectCode);
        if (subject == null)
        {
            return NotFound(new { message = "Subject not found." });
        }

        return Ok(subject);
    }

    [HttpGet("topics/{topicCode}/questions")]
    public async Task<IActionResult> GetTopicQuestions(string topicCode)
    {
        if (!UserHasPremiumAccess())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Premium subscription is required for quiz access." });
        }

        var topicRequiresSubscription = await _learningService.TopicRequiresSubscriptionAsync(topicCode);
        if (!topicRequiresSubscription.HasValue)
        {
            return NotFound(new { message = "Topic not found." });
        }

        var questions = await _learningService.GetTopicQuestionsAsync(topicCode);
        if (questions == null)
        {
            return NotFound(new { message = "Topic not found." });
        }

        return Ok(questions);
    }

    [HttpPost("exam/questions")]
    public async Task<IActionResult> GetExamQuestions([FromBody] ExamQuestionRequest request)
    {
        if (!UserHasPremiumAccess())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Premium subscription is required for real-time quiz." });
        }

        if (request?.SubjectCodes == null || request.SubjectCodes.Count == 0)
        {
            return BadRequest(new { message = "Subject codes are required." });
        }

        if (request.SubjectCodes.Count != 1)
        {
            return BadRequest(new { message = "Exactly one subject code must be provided." });
        }

        var (questionCount, _) = GetConfiguredExamSettings();

        var questions = await _learningService.GetRandomExamQuestionsAsync(
            request.SubjectCodes,
            questionCount,
            true);
        return Ok(questions);
    }

    [HttpPost("exam/evaluate")]
    public async Task<IActionResult> EvaluateExam([FromBody] ExamEvaluationRequest request)
    {
        if (!UserHasPremiumAccess())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Premium subscription is required for real-time quiz." });
        }

        if (request?.Questions == null || request.Questions.Count == 0)
        {
            return BadRequest(new { message = "Questions payload is required." });
        }

        if (request.Answers == null)
        {
            return BadRequest(new { message = "Answers payload is required." });
        }

        var result = await _learningService.EvaluateExamAnswersAsync(
            request.Questions,
            request.Answers,
            request.TotalQuestions ?? 0,
            request.IncludeExplanations);

        return Ok(result);
    }

    private bool UserHasPremiumAccess()
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        var subscriptionStatus = User.FindFirstValue("subscription_status");
        if (!string.Equals(subscriptionStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expiresAtRaw = User.FindFirstValue("subscription_expires_at");
        if (string.IsNullOrWhiteSpace(expiresAtRaw))
        {
            return true;
        }

        return DateTime.TryParse(expiresAtRaw, out var expiresAtUtc) && expiresAtUtc.ToUniversalTime() > DateTime.UtcNow;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private async Task<IActionResult> MarkTopicCompletedInternal(string topicCode, int? scorePercent)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var marked = await _learningService.MarkTopicCompletedAsync(userId.Value, topicCode, scorePercent);
        if (!marked)
        {
            return NotFound(new { message = "Topic not found." });
        }

        return Ok(new { success = true });
    }

    private (int QuestionCount, int DurationMinutes) GetConfiguredExamSettings()
    {
        var questionCount = _configuration.GetValue<int?>("Learning:Exam:QuestionCount") ?? 10;
        var durationMinutes = _configuration.GetValue<int?>("Learning:Exam:DurationMinutes") ?? 60;

        return (Math.Max(1, questionCount), Math.Max(1, durationMinutes));
    }
}

public class ExamQuestionRequest
{
    public List<string> SubjectCodes { get; set; } = new();
    public int? Count { get; set; }
}

public class ExamEvaluationRequest
{
    public List<ExamQuestionRequestDto> Questions { get; set; } = new();
    public List<ExamAnswerDto> Answers { get; set; } = new();
    public int? TotalQuestions { get; set; }
    public bool IncludeExplanations { get; set; }
}

public class ExamConfigResponse
{
    public int QuestionCount { get; set; }
    public int DurationMinutes { get; set; }
}

public class TopicCompletionRequest
{
    public string TopicCode { get; set; } = string.Empty;
    public int? ScorePercent { get; set; }
}
