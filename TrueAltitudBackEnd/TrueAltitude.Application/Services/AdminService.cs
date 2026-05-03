using TrueAltitude.Application.DTOs;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace TrueAltitude.Application.Services;

public interface IAdminService
{
    // User Management
    Task<PaginatedResponse<AdminUserDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, string? searchQuery = null, string? role = null);
    Task<AdminUserDto?> GetUserByIdAsync(int userId);
    Task<bool> UpdateUserRoleAsync(int userId, string role);
    Task<bool> ToggleUserStatusAsync(int userId, bool isActive);

    // Subject Management
    Task<SubjectResponseDto> CreateSubjectAsync(CreateSubjectDto dto);
    Task<SubjectResponseDto?> UpdateSubjectAsync(UpdateSubjectDto dto);
    Task<bool> DeleteSubjectAsync(int subjectId);
    Task<List<SubjectResponseDto>> GetAllSubjectsAsync();
    Task<SubjectResponseDto?> GetSubjectByIdAsync(int subjectId);

    // Topic Management
    Task<TopicResponseDto> CreateTopicAsync(CreateTopicDto dto);
    Task<TopicResponseDto?> UpdateTopicAsync(UpdateTopicDto dto);
    Task<bool> DeleteTopicAsync(int topicId);
    Task<List<TopicResponseDto>> GetTopicsBySubjectIdAsync(int subjectId);
    Task<TopicResponseDto?> GetTopicByIdAsync(int topicId);

    // Question Management
    Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto);
    Task<QuestionResponseDto?> UpdateQuestionAsync(UpdateQuestionDto dto);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<QuestionResponseDto?> GetQuestionByIdAsync(int questionId);
    Task<PaginatedResponse<QuestionResponseDto>> GetAllQuestionsAsync(int page = 1, int pageSize = 20, string? searchQuery = null);

    // Link Question to Topic
    Task<bool> LinkQuestionToTopicAsync(int topicId, int questionId, int sortOrder);
    Task<bool> UnlinkQuestionFromTopicAsync(int topicId, int questionId);
}

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly ILearningRepository _learningRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUserRepository userRepository,
        ILearningRepository learningRepository,
        ILogger<AdminService> logger)
    {
        _userRepository = userRepository;
        _learningRepository = learningRepository;
        _logger = logger;
    }

    #region User Management

    public async Task<PaginatedResponse<AdminUserDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, string? searchQuery = null, string? role = null)
    {
        try
        {
            var users = await _userRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedSearch = searchQuery.Trim().ToLowerInvariant();
                users = users.Where(u =>
                        u.Name.ToLower().Contains(normalizedSearch) ||
                        u.Email.ToLower().Contains(normalizedSearch))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => string.Equals(u.Role.ToString(), role, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var totalCount = users.Count;
            var skip = (page - 1) * pageSize;

            var paginatedUsers = users
                .OrderByDescending(u => u.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(MapToAdminUserDto)
                .ToList();

            return new PaginatedResponse<AdminUserDto>
            {
                Data = paginatedUsers,
                Total = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all users");
            return new PaginatedResponse<AdminUserDto>();
        }
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null ? MapToAdminUserDto(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, string role)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            if (Enum.TryParse<UserRole>(role, out var userRole))
            {
                user.Role = userRole;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ToggleUserStatusAsync(int userId, bool isActive)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region Subject Management

    public async Task<SubjectResponseDto> CreateSubjectAsync(CreateSubjectDto dto)
    {
        try
        {
            var subject = new LearningSubject
            {
                Code = dto.Code,
                Title = dto.Title,
                Description = dto.Description,
                RequiresSubscription = dto.RequiresSubscription,
                SubscriptionLabel = dto.SubscriptionLabel,
                SortOrder = dto.SortOrder
            };

            var createdSubject = await _learningRepository.CreateSubjectAsync(subject);
            return MapToSubjectResponseDto(createdSubject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject");
            throw;
        }
    }

    public async Task<SubjectResponseDto?> UpdateSubjectAsync(UpdateSubjectDto dto)
    {
        try
        {
            var subject = await _learningRepository.GetSubjectByIdAsync(dto.Id);
            if (subject == null)
                return null;

            subject.Code = dto.Code;
            subject.Title = dto.Title;
            subject.Description = dto.Description;
            subject.RequiresSubscription = dto.RequiresSubscription;
            subject.SubscriptionLabel = dto.SubscriptionLabel;
            subject.SortOrder = dto.SortOrder;

            var updatedSubject = await _learningRepository.UpdateSubjectAsync(subject);
            return MapToSubjectResponseDto(updatedSubject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject {SubjectId}", dto.Id);
            return null;
        }
    }

    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        try
        {
            return await _learningRepository.DeleteSubjectAsync(subjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {SubjectId}", subjectId);
            return false;
        }
    }

    public async Task<List<SubjectResponseDto>> GetAllSubjectsAsync()
    {
        try
        {
            var subjects = await _learningRepository.GetSubjectsAsync();
            return subjects.Select(MapToSubjectResponseDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all subjects");
            return new List<SubjectResponseDto>();
        }
    }

    public async Task<SubjectResponseDto?> GetSubjectByIdAsync(int subjectId)
    {
        try
        {
            var subject = await _learningRepository.GetSubjectByIdAsync(subjectId);
            if (subject == null)
                return null;

            var topicCount = await _learningRepository.GetTopicCountBySubjectIdAsync(subjectId);
            return MapToSubjectResponseDto(subject, topicCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subject {SubjectId}", subjectId);
            return null;
        }
    }

    #endregion

    #region Topic Management

    public async Task<TopicResponseDto> CreateTopicAsync(CreateTopicDto dto)
    {
        try
        {
            if (dto.ParentTopicId.HasValue)
            {
                var parentTopic = await _learningRepository.GetTopicByIdAsync(dto.ParentTopicId.Value);
                if (parentTopic == null || parentTopic.SubjectId != dto.SubjectId)
                {
                    throw new InvalidOperationException("Parent topic must exist in the same subject.");
                }
            }

            var topic = new LearningTopic
            {
                SubjectId = dto.SubjectId,
                ParentTopicId = dto.ParentTopicId,
                Code = dto.Code,
                Title = dto.Title,
                Description = dto.Description,
                SortOrder = dto.SortOrder
            };

            var createdTopic = await _learningRepository.CreateTopicAsync(topic);
            return MapToTopicResponseDto(createdTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating topic");
            throw;
        }
    }

    public async Task<TopicResponseDto?> UpdateTopicAsync(UpdateTopicDto dto)
    {
        try
        {
            var topic = await _learningRepository.GetTopicByIdAsync(dto.Id);
            if (topic == null)
                return null;

            if (dto.ParentTopicId == dto.Id)
            {
                return null;
            }

            if (dto.ParentTopicId.HasValue)
            {
                var parentTopic = await _learningRepository.GetTopicByIdAsync(dto.ParentTopicId.Value);
                if (parentTopic == null || parentTopic.SubjectId != dto.SubjectId)
                {
                    return null;
                }
            }

            topic.SubjectId = dto.SubjectId;
            topic.ParentTopicId = dto.ParentTopicId;
            topic.Code = dto.Code;
            topic.Title = dto.Title;
            topic.Description = dto.Description;
            topic.SortOrder = dto.SortOrder;

            var updatedTopic = await _learningRepository.UpdateTopicAsync(topic);
            return MapToTopicResponseDto(updatedTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating topic {TopicId}", dto.Id);
            return null;
        }
    }

    public async Task<bool> DeleteTopicAsync(int topicId)
    {
        try
        {
            return await _learningRepository.DeleteTopicAsync(topicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting topic {TopicId}", topicId);
            return false;
        }
    }

    public async Task<List<TopicResponseDto>> GetTopicsBySubjectIdAsync(int subjectId)
    {
        try
        {
            var topics = await _learningRepository.GetTopicsBySubjectIdAsync(subjectId);
            return topics.Select(MapToTopicResponseDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching topics for subject {SubjectId}", subjectId);
            return new List<TopicResponseDto>();
        }
    }

    public async Task<TopicResponseDto?> GetTopicByIdAsync(int topicId)
    {
        try
        {
            var topic = await _learningRepository.GetTopicByIdAsync(topicId);
            return topic != null ? MapToTopicResponseDto(topic) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching topic {TopicId}", topicId);
            return null;
        }
    }

    #endregion

    #region Question Management

    public async Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto)
    {
        try
        {
            var question = new LearningQuestion
            {
                Text = dto.QuestionText,
                Type = dto.Type,
                ExplanationText = dto.ExplanationText,
                Difficulty = dto.Difficulty,
                Code = Guid.NewGuid().ToString().Substring(0, 8)
            };

            var createdQuestion = await _learningRepository.CreateQuestionAsync(question);

            // Create options
            foreach (var option in dto.Options)
            {
                var qOption = new LearningQuestionOption
                {
                    QuestionId = createdQuestion.Id,
                    Text = option.OptionText,
                    IsCorrect = option.IsCorrect,
                    Code = Guid.NewGuid().ToString().Substring(0, 8)
                };
                await _learningRepository.CreateQuestionOptionAsync(qOption);
            }

            // Reload with options
            var questionWithOptions = await _learningRepository.GetQuestionByIdWithOptionsAsync(createdQuestion.Id);
            return questionWithOptions != null ? MapToQuestionResponseDto(questionWithOptions) : MapToQuestionResponseDto(createdQuestion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question");
            throw;
        }
    }

    public async Task<QuestionResponseDto?> UpdateQuestionAsync(UpdateQuestionDto dto)
    {
        try
        {
            var question = await _learningRepository.GetQuestionByIdWithOptionsAsync(dto.Id);
            if (question == null)
                return null;

            question.Text = dto.QuestionText;
            question.Type = dto.Type;
            question.ExplanationText = dto.ExplanationText;
            question.Difficulty = dto.Difficulty;

            await _learningRepository.UpdateQuestionAsync(question);

            // Delete existing options and create new ones
            await _learningRepository.DeleteQuestionOptionsAsync(dto.Id);

            foreach (var option in dto.Options)
            {
                var qOption = new LearningQuestionOption
                {
                    QuestionId = question.Id,
                    Text = option.OptionText,
                    IsCorrect = option.IsCorrect,
                    Code = Guid.NewGuid().ToString().Substring(0, 8)
                };
                await _learningRepository.CreateQuestionOptionAsync(qOption);
            }

            var updatedQuestion = await _learningRepository.GetQuestionByIdWithOptionsAsync(dto.Id);
            return updatedQuestion != null ? MapToQuestionResponseDto(updatedQuestion) : MapToQuestionResponseDto(question);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId}", dto.Id);
            return null;
        }
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        try
        {
            return await _learningRepository.DeleteQuestionAsync(questionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId}", questionId);
            return false;
        }
    }

    public async Task<QuestionResponseDto?> GetQuestionByIdAsync(int questionId)
    {
        try
        {
            var question = await _learningRepository.GetQuestionByIdWithOptionsAsync(questionId);
            return question != null ? MapToQuestionResponseDto(question) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching question {QuestionId}", questionId);
            return null;
        }
    }

    public async Task<PaginatedResponse<QuestionResponseDto>> GetAllQuestionsAsync(int page = 1, int pageSize = 20, string? searchQuery = null)
    {
        try
        {
            var questions = await _learningRepository.GetAllQuestionsAsync();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedSearch = searchQuery.Trim().ToLowerInvariant();
                questions = questions.Where(q =>
                        q.Text.ToLower().Contains(normalizedSearch) ||
                        q.Type.ToLower().Contains(normalizedSearch) ||
                        (q.ExplanationText != null && q.ExplanationText.ToLower().Contains(normalizedSearch)))
                    .ToList();
            }

            var totalCount = questions.Count;
            var pagedQuestions = questions
                .OrderByDescending(q => q.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToQuestionResponseDto)
                .ToList();

            return new PaginatedResponse<QuestionResponseDto>
            {
                Data = pagedQuestions,
                Total = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all questions");
            return new PaginatedResponse<QuestionResponseDto>();
        }
    }

    #endregion

    #region Link Question to Topic

    public async Task<bool> LinkQuestionToTopicAsync(int topicId, int questionId, int sortOrder)
    {
        try
        {
            var topic = await _learningRepository.GetTopicByIdAsync(topicId);
            var question = await _learningRepository.GetQuestionByIdWithOptionsAsync(questionId);

            if (topic == null || question == null)
            {
                return false;
            }

            if (question.TopicQuestions.Any(tq => tq.TopicId == topicId))
            {
                return true;
            }

            var link = new LearningTopicQuestion
            {
                TopicId = topicId,
                QuestionId = questionId,
                SortOrder = sortOrder
            };

            await _learningRepository.CreateTopicQuestionAsync(link);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking question {QuestionId} to topic {TopicId}", questionId, topicId);
            return false;
        }
    }

    public async Task<bool> UnlinkQuestionFromTopicAsync(int topicId, int questionId)
    {
        try
        {
            return await _learningRepository.DeleteTopicQuestionAsync(topicId, questionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlinking question {QuestionId} from topic {TopicId}", questionId, topicId);
            return false;
        }
    }

    #endregion

    #region Mapping Methods

    private AdminUserDto MapToAdminUserDto(User user)
    {
        return new AdminUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            SubscriptionStatus = user.SubscriptionStatus,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    private SubjectResponseDto MapToSubjectResponseDto(LearningSubject subject, int topicCount = 0)
    {
        return new SubjectResponseDto
        {
            Id = subject.Id,
            Code = subject.Code,
            Title = subject.Title,
            Description = subject.Description,
            RequiresSubscription = subject.RequiresSubscription,
            SubscriptionLabel = subject.SubscriptionLabel,
            SortOrder = subject.SortOrder,
            TopicCount = topicCount > 0 ? topicCount : subject.Topics?.Count ?? 0
        };
    }

    private TopicResponseDto MapToTopicResponseDto(LearningTopic topic, int questionCount = 0)
    {
        return new TopicResponseDto
        {
            Id = topic.Id,
            SubjectId = topic.SubjectId,
            ParentTopicId = topic.ParentTopicId,
            ParentTopicTitle = topic.ParentTopic?.Title,
            Code = topic.Code,
            Title = topic.Title,
            Description = topic.Description,
            SortOrder = topic.SortOrder,
            QuestionCount = questionCount > 0 ? questionCount : topic.TopicQuestions?.Count ?? 0
        };
    }

    private QuestionResponseDto MapToQuestionResponseDto(LearningQuestion question)
    {
        return new QuestionResponseDto
        {
            Id = question.Id,
            QuestionText = question.Text,
            Type = question.Type,
            ExplanationText = question.ExplanationText,
            Difficulty = question.Difficulty,
            Options = question.Options?.Select(o => new QuestionOptionDto
            {
                Id = o.Id,
                OptionText = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList() ?? new List<QuestionOptionDto>(),
            LinkedTopics = question.TopicQuestions?.OrderBy(tq => tq.SortOrder)
                .ThenBy(tq => tq.TopicId)
                .Select(tq => new QuestionLinkedTopicDto
                {
                    Id = tq.TopicId,
                    SubjectId = tq.Topic.SubjectId,
                    ParentTopicId = tq.Topic.ParentTopicId,
                    Title = tq.Topic.Title,
                    ParentTopicTitle = tq.Topic.ParentTopic?.Title,
                    SortOrder = tq.SortOrder
                }).ToList() ?? new List<QuestionLinkedTopicDto>()
        };
    }

    #endregion
}
