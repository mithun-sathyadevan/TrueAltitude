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

    public LearningController(ILearningService learningService)
    {
        _learningService = learningService;
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> GetSubjects()
    {
        var subjects = await _learningService.GetSubjectsAsync();
        return Ok(subjects);
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
        var hasPremiumAccess = UserHasPremiumAccess();
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

        if (hasPremiumAccess)
        {
            return Ok(questions);
        }

        var maskedQuestions = questions.Select(question =>
        {
            if (!question.RequiresSubscription)
            {
                return question;
            }

            return new LearningQuestionDto
            {
                Id = question.Id,
                Text = "Premium Access Required",
                AnswerImageUrl = null,
                Explanation = string.Empty,
                RequiresSubscription = true,
                SubscriptionLabel = question.SubscriptionLabel,
                Options = new List<LearningQuestionOptionDto>()
            };
        }).ToList();

        return Ok(maskedQuestions);
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

        var questions = await _learningService.GetRandomExamQuestionsAsync(request.SubjectCodes, request.Count ?? 10);
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
