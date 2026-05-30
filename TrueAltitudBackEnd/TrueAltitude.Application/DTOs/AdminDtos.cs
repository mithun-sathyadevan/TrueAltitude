namespace TrueAltitude.Application.DTOs;

// User Management
public class AdminUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public string SubscriptionStatus { get; set; } = "none";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminSubscriptionPurchaseDto
{
    public int PurchaseId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? FailedReason { get; set; }
    public int AmountInPaise { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentProvider { get; set; } = string.Empty;
    public string? ProviderOrderId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }
}

public class UpdateUserRoleDto
{
    public int UserId { get; set; }
    public string Role { get; set; } = "Customer"; // Admin, Manager, Customer
}

public class ToggleUserStatusDto
{
    public int UserId { get; set; }
    public bool IsActive { get; set; }
}

// Subject Management
public class CreateSubjectDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateSubjectDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public int SortOrder { get; set; }
}

public class SubjectResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public int SortOrder { get; set; }
    public int TopicCount { get; set; }
}

// Topic Management
public class CreateTopicDto
{
    public int SubjectId { get; set; }
    public int? ParentTopicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateTopicDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? ParentTopicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class TopicResponseDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? ParentTopicId { get; set; }
    public string? ParentTopicTitle { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int QuestionCount { get; set; }
}

public class QuestionLinkedTopicDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? ParentTopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ParentTopicTitle { get; set; }
    public int SortOrder { get; set; }
}

// Question Management
public class CreateQuestionDto
{
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = "multiple_choice"; // multiple_choice, true_false, short_answer
    public string? AnswerImageUrl { get; set; }
    public string? ExplanationText { get; set; }
    public int Difficulty { get; set; } // 1-5
    public List<CreateQuestionOptionDto> Options { get; set; } = new();
}

public class CreateQuestionOptionDto
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class UpdateQuestionDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = "multiple_choice";
    public string? AnswerImageUrl { get; set; }
    public string? ExplanationText { get; set; }
    public int Difficulty { get; set; }
    public List<CreateQuestionOptionDto> Options { get; set; } = new();
}

public class QuestionResponseDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? AnswerImageUrl { get; set; }
    public string? ExplanationText { get; set; }
    public int Difficulty { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = new();
    public List<QuestionLinkedTopicDto> LinkedTopics { get; set; } = new();
}

public class QuestionOptionDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

// Link Question to Topic
public class LinkQuestionToTopicDto
{
    public int TopicId { get; set; }
    public int QuestionId { get; set; }
    public int SortOrder { get; set; }
}

public class UnlinkQuestionFromTopicDto
{
    public int TopicId { get; set; }
    public int QuestionId { get; set; }
}

public class BulkQuestionImportItemDto
{
    public int RowNumber { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = "multiple_choice";
    public int Difficulty { get; set; } = 1;
    public string? ExplanationText { get; set; }
    public string? AnswerImageUrl { get; set; }
    public string CorrectOption { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
}

public class BulkQuestionImportResultDto
{
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int CreatedQuestions { get; set; }
    public int ReusedQuestions { get; set; }
    public int LinkedToTopic { get; set; }
    public int AlreadyLinked { get; set; }
    public int SkippedRows { get; set; }
    public List<string> Errors { get; set; } = new();
}

// Response Wrappers
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}
