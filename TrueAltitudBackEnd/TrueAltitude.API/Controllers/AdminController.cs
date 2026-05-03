using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Application.Services;
using System.Security.Claims;

namespace TrueAltitude.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has admin privileges
    /// </summary>
    private bool IsAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        return roleClaim?.Value == "Admin";
    }

    #region User Management Endpoints

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? searchQuery = null, [FromQuery] string? role = null)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.GetAllUsersAsync(page, pageSize, searchQuery, role);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return BadRequest(new { success = false, message = "Error fetching users." });
        }
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var user = await _adminService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            return Ok(new { success = true, data = user });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", userId);
            return BadRequest(new { success = false, message = "Error fetching user." });
        }
    }

    /// <summary>
    /// Update user role (Admin only)
    /// </summary>
    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (userId != dto.UserId)
        {
            return BadRequest(new { success = false, message = "User ID mismatch." });
        }

        try
        {
            var result = await _adminService.UpdateUserRoleAsync(userId, dto.Role);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to update user role." });
            }

            return Ok(new { success = true, message = "User role updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role for user {UserId}", userId);
            return BadRequest(new { success = false, message = "Error updating user role." });
        }
    }

    /// <summary>
    /// Toggle user active status (Admin only)
    /// </summary>
    [HttpPut("users/{userId}/status")]
    public async Task<IActionResult> ToggleUserStatus(int userId, [FromBody] ToggleUserStatusDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.ToggleUserStatusAsync(userId, dto.IsActive);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to update user status." });
            }

            return Ok(new { success = true, message = "User status updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for user {UserId}", userId);
            return BadRequest(new { success = false, message = "Error updating user status." });
        }
    }

    #endregion

    #region Subject Management Endpoints

    /// <summary>
    /// Create a new subject (Admin only)
    /// </summary>
    [HttpPost("subjects")]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { success = false, message = "Code and Title are required." });
        }

        try
        {
            var result = await _adminService.CreateSubjectAsync(dto);
            return CreatedAtAction(nameof(GetSubjectById), new { subjectId = result.Id }, new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject");
            return BadRequest(new { success = false, message = "Error creating subject." });
        }
    }

    /// <summary>
    /// Get all subjects
    /// </summary>
    [HttpGet("subjects")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllSubjects()
    {
        try
        {
            var result = await _adminService.GetAllSubjectsAsync();
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subjects");
            return BadRequest(new { success = false, message = "Error fetching subjects." });
        }
    }

    /// <summary>
    /// Get subject by ID
    /// </summary>
    [HttpGet("subjects/{subjectId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSubjectById(int subjectId)
    {
        try
        {
            var result = await _adminService.GetSubjectByIdAsync(subjectId);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Subject not found." });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subject {SubjectId}", subjectId);
            return BadRequest(new { success = false, message = "Error fetching subject." });
        }
    }

    /// <summary>
    /// Update subject (Admin only)
    /// </summary>
    [HttpPut("subjects/{subjectId}")]
    public async Task<IActionResult> UpdateSubject(int subjectId, [FromBody] UpdateSubjectDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (subjectId != dto.Id)
        {
            return BadRequest(new { success = false, message = "Subject ID mismatch." });
        }

        try
        {
            var result = await _adminService.UpdateSubjectAsync(dto);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Subject not found." });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject {SubjectId}", subjectId);
            return BadRequest(new { success = false, message = "Error updating subject." });
        }
    }

    /// <summary>
    /// Delete subject (Admin only)
    /// </summary>
    [HttpDelete("subjects/{subjectId}")]
    public async Task<IActionResult> DeleteSubject(int subjectId)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.DeleteSubjectAsync(subjectId);
            if (!result)
            {
                return NotFound(new { success = false, message = "Subject not found." });
            }

            return Ok(new { success = true, message = "Subject deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {SubjectId}", subjectId);
            return BadRequest(new { success = false, message = "Error deleting subject." });
        }
    }

    #endregion

    #region Topic Management Endpoints

    /// <summary>
    /// Create a new topic (Admin only)
    /// </summary>
    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { success = false, message = "Code and Title are required." });
        }

        try
        {
            var result = await _adminService.CreateTopicAsync(dto);
            return CreatedAtAction(nameof(GetTopicById), new { topicId = result.Id }, new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating topic");
            return BadRequest(new { success = false, message = "Error creating topic." });
        }
    }

    /// <summary>
    /// Get topics by subject ID
    /// </summary>
    [HttpGet("subjects/{subjectId}/topics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTopicsBySubjectId(int subjectId)
    {
        try
        {
            var result = await _adminService.GetTopicsBySubjectIdAsync(subjectId);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching topics for subject {SubjectId}", subjectId);
            return BadRequest(new { success = false, message = "Error fetching topics." });
        }
    }

    /// <summary>
    /// Get topic by ID
    /// </summary>
    [HttpGet("topics/{topicId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTopicById(int topicId)
    {
        try
        {
            var result = await _adminService.GetTopicByIdAsync(topicId);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Topic not found." });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching topic {TopicId}", topicId);
            return BadRequest(new { success = false, message = "Error fetching topic." });
        }
    }

    /// <summary>
    /// Update topic (Admin only)
    /// </summary>
    [HttpPut("topics/{topicId}")]
    public async Task<IActionResult> UpdateTopic(int topicId, [FromBody] UpdateTopicDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (topicId != dto.Id)
        {
            return BadRequest(new { success = false, message = "Topic ID mismatch." });
        }

        try
        {
            var result = await _adminService.UpdateTopicAsync(dto);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Topic not found." });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating topic {TopicId}", topicId);
            return BadRequest(new { success = false, message = "Error updating topic." });
        }
    }

    /// <summary>
    /// Delete topic (Admin only)
    /// </summary>
    [HttpDelete("topics/{topicId}")]
    public async Task<IActionResult> DeleteTopic(int topicId)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.DeleteTopicAsync(topicId);
            if (!result)
            {
                return NotFound(new { success = false, message = "Topic not found." });
            }

            return Ok(new { success = true, message = "Topic deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting topic {TopicId}", topicId);
            return BadRequest(new { success = false, message = "Error deleting topic." });
        }
    }

    #endregion

    #region Question Management Endpoints

    /// <summary>
    /// Create a new question (Admin only)
    /// </summary>
    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (string.IsNullOrWhiteSpace(dto.QuestionText) || dto.Options.Count == 0)
        {
            return BadRequest(new { success = false, message = "Question text and options are required." });
        }

        try
        {
            var result = await _adminService.CreateQuestionAsync(dto);
            return CreatedAtAction(nameof(GetQuestionById), new { questionId = result.Id }, new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            return BadRequest(new { success = false, message = "Error creating question." });
        }
    }

    /// <summary>
    /// Get all questions (Admin only)
    /// </summary>
    [HttpGet("questions")]
    public async Task<IActionResult> GetAllQuestions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? searchQuery = null)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.GetAllQuestionsAsync(page, pageSize, searchQuery);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching questions");
            return BadRequest(new { success = false, message = "Error fetching questions." });
        }
    }

    /// <summary>
    /// Get question by ID
    /// </summary>
    [HttpGet("questions/{questionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQuestionById(int questionId)
    {
        try
        {
            var result = await _adminService.GetQuestionByIdAsync(questionId);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Question not found." });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching question {QuestionId}", questionId);
            return BadRequest(new { success = false, message = "Error fetching question." });
        }
    }

    /// <summary>
    /// Update question (Admin only)
    /// </summary>
    [HttpPut("questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] UpdateQuestionDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (questionId != dto.Id)
        {
            return BadRequest(new { success = false, message = "Question ID mismatch." });
        }

        try
        {
            var result = await _adminService.UpdateQuestionAsync(dto);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Question not found." });
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", questionId);
            return BadRequest(new { success = false, message = "Error updating question." });
        }
    }

    /// <summary>
    /// Delete question (Admin only)
    /// </summary>
    [HttpDelete("questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.DeleteQuestionAsync(questionId);
            if (!result)
            {
                return NotFound(new { success = false, message = "Question not found." });
            }

            return Ok(new { success = true, message = "Question deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
            return BadRequest(new { success = false, message = "Error deleting question." });
        }
    }

    #endregion

    #region Link Question to Topic

    /// <summary>
    /// Link question to topic (Admin only)
    /// </summary>
    [HttpPost("topics/{topicId}/questions/{questionId}")]
    public async Task<IActionResult> LinkQuestionToTopic(int topicId, int questionId, [FromBody] LinkQuestionToTopicDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.LinkQuestionToTopicAsync(topicId, questionId, dto.SortOrder);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to link question to topic." });
            }

            return Ok(new { success = true, message = "Question linked to topic successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking question {QuestionId} to topic {TopicId}", questionId, topicId);
            return BadRequest(new { success = false, message = "Error linking question to topic." });
        }
    }

    /// <summary>
    /// Unlink question from topic (Admin only)
    /// </summary>
    [HttpDelete("topics/{topicId}/questions/{questionId}")]
    public async Task<IActionResult> UnlinkQuestionFromTopic(int topicId, int questionId)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.UnlinkQuestionFromTopicAsync(topicId, questionId);
            if (!result)
            {
                return BadRequest(new { success = false, message = "Failed to unlink question from topic." });
            }

            return Ok(new { success = true, message = "Question unlinked from topic successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking question {QuestionId} from topic {TopicId}", questionId, topicId);
            return BadRequest(new { success = false, message = "Error unlinking question from topic." });
        }
    }

    #endregion
}
