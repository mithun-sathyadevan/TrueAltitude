namespace TrueAltitude.Application.DTOs;

public class LearningSubjectListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
}

public class LearningTopicNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public List<LearningTopicNodeDto> Children { get; set; } = new();
    public List<LearningQuestionDto> Questions { get; set; } = new();
}

public class LearningQuestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? AnswerImageUrl { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public bool RequiresSubscription { get; set; }
    public string? SubscriptionLabel { get; set; }
    public List<LearningQuestionOptionDto> Options { get; set; } = new();
}

public class LearningQuestionOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class ExamQuestionDto
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<ExamQuestionOptionDto> Options { get; set; } = new();
}

public class ExamQuestionOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ExamAnswerDto
{
    public string QuestionId { get; set; } = string.Empty;
    public string OptionId { get; set; } = string.Empty;
}

public class ExamQuestionRequestDto
{
    public string QuestionId { get; set; } = string.Empty;
}

public class ExamEvaluationResultDto
{
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int Percent { get; set; }
    public List<ExamQuestionEvaluationDto> Results { get; set; } = new();
}

public class ExamQuestionEvaluationDto
{
    public string QuestionId { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsAnswered { get; set; }
    public string CorrectOptionId { get; set; } = string.Empty;
    public string CorrectOptionText { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string? AnswerImageUrl { get; set; }
}
