// User Management Models
export interface AdminUser {
  id: number;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  isEmailVerified: boolean;
  subscriptionStatus: string;
  createdAt: Date | string;
  lastLoginAt?: Date | string | null;
}

export interface UpdateUserRoleRequest {
  userId: number;
  role: string;
}

export interface ToggleUserStatusRequest {
  userId: number;
  isActive: boolean;
}

// Subject Management Models
export interface Subject {
  id: number;
  code: string;
  title: string;
  description: string;
  requiresSubscription: boolean;
  subscriptionLabel?: string;
  sortOrder: number;
  topicCount?: number;
}

export interface CreateSubjectRequest {
  code: string;
  title: string;
  description: string;
  requiresSubscription: boolean;
  subscriptionLabel?: string;
  sortOrder: number;
}

export interface UpdateSubjectRequest extends CreateSubjectRequest {
  id: number;
}

// Topic Management Models
export interface Topic {
  id: number;
  subjectId: number;
  parentTopicId?: number | null;
  parentTopicTitle?: string | null;
  code: string;
  title: string;
  description: string;
  sortOrder: number;
  questionCount?: number;
}

export interface CreateTopicRequest {
  subjectId: number;
  parentTopicId?: number | null;
  code: string;
  title: string;
  description: string;
  sortOrder: number;
}

export interface UpdateTopicRequest extends CreateTopicRequest {
  id: number;
}

// Question Management Models
export interface QuestionOption {
  id: number;
  optionText: string;
  isCorrect: boolean;
}

export interface LinkedTopic {
  id: number;
  subjectId: number;
  parentTopicId?: number | null;
  title: string;
  parentTopicTitle?: string | null;
  sortOrder: number;
}

export interface Question {
  id: number;
  questionText: string;
  type: string;
  explanationText?: string;
  difficulty: number;
  options: QuestionOption[];
  linkedTopics: LinkedTopic[];
}

export interface CreateQuestionOptionRequest {
  optionText: string;
  isCorrect: boolean;
}

export interface CreateQuestionRequest {
  questionText: string;
  type: string;
  explanationText?: string;
  difficulty: number;
  options: CreateQuestionOptionRequest[];
}

export interface UpdateQuestionRequest extends CreateQuestionRequest {
  id: number;
}

// Linking Models
export interface LinkQuestionRequest {
  topicId: number;
  questionId: number;
  sortOrder: number;
}

// Pagination Models
export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  pageSize: number;
  currentPage: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
}
