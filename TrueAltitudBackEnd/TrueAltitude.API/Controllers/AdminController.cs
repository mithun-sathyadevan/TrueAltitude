using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrueAltitude.Application.DTOs;
using TrueAltitude.Application.Services;
using System.Security.Claims;
using TrueAltitude.Persistence.Data;

namespace TrueAltitude.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<AdminController> _logger;
    private readonly TrueAltitudeDbContext _dbContext;
    private readonly string? _blobConnectionString;
    private readonly string _blobContainerName;

    public AdminController(
        IAdminService adminService,
        ISubscriptionService subscriptionService,
        ILogger<AdminController> logger,
        IConfiguration configuration,
        TrueAltitudeDbContext dbContext)
    {
        _adminService = adminService;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _dbContext = dbContext;
        _blobConnectionString = configuration["AzureBlobStorage:ConnectionString"];
        _blobContainerName = configuration["AzureBlobStorage:ContainerName"] ?? "question-media";
    }

    /// <summary>
    /// Check if user has admin privileges
    /// </summary>
    private bool IsAdmin()
    {
        return User.IsInRole("Admin")
            || string.Equals(User.FindFirstValue(ClaimTypes.Role), "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(User.FindFirstValue("role"), "Admin", StringComparison.OrdinalIgnoreCase);
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
    /// Get subscription purchases with user details (Admin only)
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptionPurchases([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? searchQuery = null, [FromQuery] string? status = null)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var result = await _adminService.GetSubscriptionPurchasesAsync(page, pageSize, searchQuery, status);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription purchases");
            return BadRequest(new { success = false, message = "Error fetching subscription purchases." });
        }
    }

    /// <summary>
    /// Get subscription plans configuration (Admin only)
    /// </summary>
    [HttpGet("subscription-plans")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        try
        {
            var plans = await _subscriptionService.GetPlansForAdminAsync();
            return Ok(new { success = true, data = plans });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription plans configuration");
            return BadRequest(new { success = false, message = "Error fetching subscription plans." });
        }
    }

    /// <summary>
    /// Update subscription plans configuration (Admin only)
    /// </summary>
    [HttpPut("subscription-plans")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSubscriptionPlans([FromBody] UpdateSubscriptionPlansRequestDto dto)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (dto?.Plans == null || dto.Plans.Count == 0)
        {
            return BadRequest(new { success = false, message = "At least one subscription plan is required." });
        }

        try
        {
            var plans = await _subscriptionService.UpdatePlansAsync(dto.Plans);
            return Ok(new { success = true, message = "Subscription plans updated successfully.", data = plans });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plans configuration");
            return BadRequest(new { success = false, message = "Error updating subscription plans." });
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
    /// Upload question image to Azure Blob Storage (Admin only).
    /// </summary>
    [HttpPost("questions/images/upload")]
    [HttpPost("questions/images/fake-upload")]
    public async Task<IActionResult> UploadQuestionImage([FromForm] IFormFile? file)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Image file is required." });
        }

        if (string.IsNullOrWhiteSpace(_blobConnectionString))
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Azure Blob connection string is not configured. Set AzureBlobStorage:ConnectionString in appsettings."
            });
        }

        try
        {
            var blobServiceClient = new BlobServiceClient(_blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var fileExtension = Path.GetExtension(file.FileName);
            var blobName = $"questions/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{fileExtension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await using var fileStream = file.OpenReadStream();
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
                }
            });

            return Ok(new
            {
                success = true,
                data = new
                {
                    url = blobClient.Uri.ToString(),
                    originalName = file.FileName,
                    size = file.Length
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading question image to Azure Blob Storage");
            return StatusCode(500, new { success = false, message = "Failed to upload image to Azure Blob Storage." });
        }
    }

    /// <summary>
    /// Bulk upload questions from an Excel file and link them to a topic (Admin only).
    /// </summary>
    [HttpPost("topics/{topicId}/questions/bulk-upload")]
    public async Task<IActionResult> BulkUploadQuestionsToTopic(int topicId, [FromForm] IFormFile? file)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Excel file is required." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            return BadRequest(new { success = false, message = "Only .xlsx or .xls files are supported." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return BadRequest(new { success = false, message = "Excel file does not contain any worksheet." });
            }

            if (!TryParseWorksheetRows(worksheet, out var importRows, out var parseError))
            {
                return BadRequest(new { success = false, message = parseError ?? "Failed to parse worksheet." });
            }

            if (importRows.Count == 0)
            {
                return BadRequest(new { success = false, message = "No valid question rows found in Excel." });
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                var result = await _adminService.BulkImportQuestionsToTopicAsync(topicId, importRows);

                if (result.Errors.Count > 0 || result.SkippedRows > 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Excel import failed. No data was saved.",
                        errors = result.Errors
                    });
                }

                await transaction.CommitAsync();
                return Ok(new { success = true, data = result });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk importing questions for topic {TopicId}", topicId);
            return StatusCode(500, new { success = false, message = "Failed to process Excel file." });
        }
    }

    /// <summary>
    /// Upload a workbook where file name becomes subject and each worksheet becomes a topic (Admin only).
    /// </summary>
    [HttpPost("questions/workbook-import")]
    public async Task<IActionResult> BulkUploadWorkbook([FromForm] IFormFile? file)
    {
        if (!IsAdmin())
        {
            return Forbid("Only admins can access this endpoint.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "Excel file is required." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            return BadRequest(new { success = false, message = "Only .xlsx or .xls files are supported." });
        }

        var subjectTitle = Path.GetFileNameWithoutExtension(file.FileName).Trim();
        if (string.IsNullOrWhiteSpace(subjectTitle))
        {
            return BadRequest(new { success = false, message = "Could not determine subject title from Excel file name." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);

            if (!workbook.Worksheets.Any())
            {
                return BadRequest(new { success = false, message = "Excel file does not contain any worksheet." });
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                var allSubjects = await _adminService.GetAllSubjectsAsync();
                var existingSubject = allSubjects.FirstOrDefault(s =>
                    string.Equals(s.Title.Trim(), subjectTitle, StringComparison.OrdinalIgnoreCase));

                var createdSubject = false;
                var subject = existingSubject;

                if (subject == null)
                {
                    var existingSubjectCodes = new HashSet<string>(
                        allSubjects.Select(s => s.Code),
                        StringComparer.OrdinalIgnoreCase);

                    var created = await _adminService.CreateSubjectAsync(new CreateSubjectDto
                    {
                        Code = GenerateUniqueCode(subjectTitle, existingSubjectCodes, "SUB"),
                        Title = subjectTitle,
                        Description = $"Imported from workbook {file.FileName} on {DateTime.UtcNow:yyyy-MM-dd}.",
                        RequiresSubscription = false,
                        SortOrder = (allSubjects.Select(s => s.SortOrder).DefaultIfEmpty(0).Max()) + 1
                    });

                    subject = created;
                    createdSubject = true;
                }

                var result = new WorkbookQuestionImportResultDto
                {
                    SubjectId = subject.Id,
                    SubjectCode = subject.Code,
                    SubjectTitle = subject.Title,
                    SubjectCreated = createdSubject
                };

                var topics = await _adminService.GetTopicsBySubjectIdAsync(subject.Id);
                var topicsByTitle = topics
                    .GroupBy(t => t.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var topicCodes = new HashSet<string>(topics.Select(t => t.Code), StringComparer.OrdinalIgnoreCase);
                var nextTopicSortOrder = topics.Select(t => t.SortOrder).DefaultIfEmpty(0).Max() + 1;
                var importErrors = new List<string>();

                foreach (var worksheet in workbook.Worksheets)
                {
                    var sheetTitle = worksheet.Name?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(sheetTitle))
                    {
                        continue;
                    }

                    if (!TryParseWorksheetRows(worksheet, out var importRows, out var parseError))
                    {
                        importErrors.Add($"Worksheet '{sheetTitle}': {parseError ?? "Failed to parse worksheet."}");
                        break;
                    }

                    if (importRows.Count == 0)
                    {
                        importErrors.Add($"Worksheet '{sheetTitle}': No valid question rows found in this worksheet.");
                        break;
                    }

                    var topicCreated = false;
                    if (!topicsByTitle.TryGetValue(sheetTitle, out var topic))
                    {
                        topic = await _adminService.CreateTopicAsync(new CreateTopicDto
                        {
                            SubjectId = subject.Id,
                            ParentTopicId = null,
                            Code = GenerateUniqueCode(sheetTitle, topicCodes, "TOP"),
                            Title = sheetTitle,
                            Description = $"Imported from worksheet {sheetTitle}.",
                            SortOrder = nextTopicSortOrder++
                        });

                        topicsByTitle[sheetTitle] = topic;
                        topicCodes.Add(topic.Code);
                        topicCreated = true;
                    }

                    var topicImport = await _adminService.BulkImportQuestionsToTopicAsync(topic.Id, importRows);

                    if (topicImport.Errors.Count > 0 || topicImport.SkippedRows > 0)
                    {
                        importErrors.Add($"Worksheet '{sheetTitle}': Import failed.");
                        importErrors.AddRange(topicImport.Errors.Select(error => $"Worksheet '{sheetTitle}': {error}"));
                        break;
                    }

                    result.TotalRows += topicImport.TotalRows;
                    result.ProcessedRows += topicImport.ProcessedRows;
                    result.CreatedQuestions += topicImport.CreatedQuestions;
                    result.ReusedQuestions += topicImport.ReusedQuestions;
                    result.LinkedToTopic += topicImport.LinkedToTopic;
                    result.AlreadyLinked += topicImport.AlreadyLinked;
                    result.SkippedRows += topicImport.SkippedRows;

                    result.Topics.Add(new WorkbookTopicImportResultDto
                    {
                        TopicId = topic.Id,
                        TopicCode = topic.Code,
                        TopicTitle = topic.Title,
                        TopicCreated = topicCreated,
                        TotalRows = topicImport.TotalRows,
                        ProcessedRows = topicImport.ProcessedRows,
                        CreatedQuestions = topicImport.CreatedQuestions,
                        ReusedQuestions = topicImport.ReusedQuestions,
                        LinkedToTopic = topicImport.LinkedToTopic,
                        AlreadyLinked = topicImport.AlreadyLinked,
                        SkippedRows = topicImport.SkippedRows,
                        Errors = topicImport.Errors
                    });
                }

                if (importErrors.Count > 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Workbook import failed. No data was saved.",
                        errors = importErrors
                    });
                }

                await transaction.CommitAsync();

                return Ok(new { success = true, data = result });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing workbook {FileName}", file.FileName);

            var detail = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to process Excel workbook.",
                detail,
                traceId = HttpContext.TraceIdentifier
            });
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

    private static string NormalizeHeader(string header)
    {
        return new string(header
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static int? FindColumn(Dictionary<string, int> map, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out var column))
            {
                return column;
            }
        }

        return null;
    }

    private static string GetCellValue(IXLWorksheet worksheet, int row, int? column)
    {
        if (!column.HasValue)
        {
            return string.Empty;
        }

        return worksheet.Cell(row, column.Value).GetString().Trim();
    }

    private static int ExtractOptionOrder(string normalizedHeader)
    {
        var suffix = normalizedHeader.Replace("option", string.Empty);
        if (int.TryParse(suffix, out var number))
        {
            return number;
        }

        if (!string.IsNullOrWhiteSpace(suffix) && suffix.Length == 1 && char.IsLetter(suffix[0]))
        {
            return (char.ToUpperInvariant(suffix[0]) - 'A') + 1;
        }

        return int.MaxValue;
    }

    private static bool IsOptionHeader(string normalizedHeader)
    {
        if (string.IsNullOrWhiteSpace(normalizedHeader))
        {
            return false;
        }

        if (normalizedHeader.StartsWith("option") && !normalizedHeader.Contains("correct"))
        {
            return true;
        }

        if (normalizedHeader.StartsWith("choice") && !normalizedHeader.Contains("correct"))
        {
            return true;
        }

        return normalizedHeader.Length == 1 && char.IsLetter(normalizedHeader[0]);
    }

    private static bool TryParseWorksheetRows(
        IXLWorksheet worksheet,
        out List<BulkQuestionImportItemDto> importRows,
        out string? errorMessage)
    {
        importRows = new List<BulkQuestionImportItemDto>();
        errorMessage = null;

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
        {
            errorMessage = "Excel worksheet is empty.";
            return false;
        }

        var headerRow = worksheet.Row(1);
        var lastColumn = usedRange.LastColumn().ColumnNumber();

        var columnMap = new Dictionary<string, int>();
        var optionColumns = new List<(string Header, int Column)>();

        for (var col = 1; col <= lastColumn; col++)
        {
            var rawHeader = headerRow.Cell(col).GetString();
            if (string.IsNullOrWhiteSpace(rawHeader))
            {
                continue;
            }

            var normalizedHeader = NormalizeHeader(rawHeader);
            columnMap[normalizedHeader] = col;

            if (IsOptionHeader(normalizedHeader))
            {
                optionColumns.Add((normalizedHeader, col));
            }
        }

        if (optionColumns.Count == 0)
        {
            errorMessage = "No option columns found. Add headers like Option1, Option2, Option3...";
            return false;
        }

        var sortedOptionColumns = optionColumns
            .OrderBy(o => ExtractOptionOrder(o.Header))
            .ToList();

        var lastRow = usedRange.LastRow().RowNumber();
        for (var row = 2; row <= lastRow; row++)
        {
            var questionText = GetCellValue(worksheet, row, FindColumn(columnMap, "questiontext", "question", "text"));
            if (string.IsNullOrWhiteSpace(questionText))
            {
                continue;
            }

            var options = sortedOptionColumns
                .Select(c => worksheet.Cell(row, c.Column).GetString().Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            var type = GetCellValue(worksheet, row, FindColumn(columnMap, "type", "questiontype"));
            var difficultyRaw = GetCellValue(worksheet, row, FindColumn(columnMap, "difficulty", "level"));
            _ = int.TryParse(difficultyRaw, out var difficulty);

            importRows.Add(new BulkQuestionImportItemDto
            {
                RowNumber = row,
                QuestionText = questionText,
                Type = string.IsNullOrWhiteSpace(type) ? "multiple_choice" : type,
                Difficulty = difficulty <= 0 ? 1 : difficulty,
                ExplanationText = GetCellValue(worksheet, row, FindColumn(columnMap, "explanation", "explanationtext", "solution", "notes", "note")),
                AnswerImageUrl = GetCellValue(worksheet, row, FindColumn(columnMap, "answerimageurl", "imageurl", "image", "questionimageurl")),
                CorrectOption = GetCellValue(worksheet, row, FindColumn(columnMap, "correctoption", "correctanswer", "answer", "correct")),
                Options = options
            });
        }

        return true;
    }

    private static string GenerateUniqueCode(string source, HashSet<string> existingCodes, string prefix)
    {
        var sourceChars = source
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray();
        var cleaned = new string(sourceChars);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = prefix;
        }

        var baseCode = cleaned.Length > 12 ? cleaned[..12] : cleaned;
        var candidate = baseCode;
        var suffix = 1;

        while (existingCodes.Contains(candidate))
        {
            candidate = $"{baseCode}{suffix}";
            suffix++;
        }

        existingCodes.Add(candidate);
        return candidate;
    }

    private sealed class WorkbookQuestionImportResultDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectTitle { get; set; } = string.Empty;
        public bool SubjectCreated { get; set; }
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int CreatedQuestions { get; set; }
        public int ReusedQuestions { get; set; }
        public int LinkedToTopic { get; set; }
        public int AlreadyLinked { get; set; }
        public int SkippedRows { get; set; }
        public List<WorkbookTopicImportResultDto> Topics { get; set; } = new();
    }

    private sealed class WorkbookTopicImportResultDto
    {
        public int TopicId { get; set; }
        public string TopicCode { get; set; } = string.Empty;
        public string TopicTitle { get; set; } = string.Empty;
        public bool TopicCreated { get; set; }
        public int TotalRows { get; set; }
        public int ProcessedRows { get; set; }
        public int CreatedQuestions { get; set; }
        public int ReusedQuestions { get; set; }
        public int LinkedToTopic { get; set; }
        public int AlreadyLinked { get; set; }
        public int SkippedRows { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
