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

export interface AdminSubscriptionPurchase {
  purchaseId: number;
  userId: number;
  userName: string;
  userEmail: string;
  planCode: string;
  planName: string;
  paymentStatus: 'pending' | 'paid' | 'failed' | string;
  failedReason?: string | null;
  amountInPaise: number;
  currency: string;
  paymentProvider: string;
  providerOrderId?: string | null;
  providerPaymentId?: string | null;
  createdAt: Date | string;
  paidAt?: Date | string | null;
  subscriptionEndsAt?: Date | string | null;
}

export interface AdminSubscriptionPlan {
  code: string;
  name: string;
  priceInPaise: number;
  durationDays: number;
  description?: string;
  isPopular: boolean;
}

export interface UpdateSubscriptionPlansRequest {
  plans: AdminSubscriptionPlan[];
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
  answerImageUrl?: string;
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
  answerImageUrl?: string;
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

export interface BulkQuestionImportResult {
  totalRows: number;
  processedRows: number;
  createdQuestions: number;
  reusedQuestions: number;
  linkedToTopic: number;
  alreadyLinked: number;
  skippedRows: number;
  errors: string[];
}

export interface WorkbookTopicImportResult {
  topicId: number;
  topicCode: string;
  topicTitle: string;
  topicCreated: boolean;
  totalRows: number;
  processedRows: number;
  createdQuestions: number;
  reusedQuestions: number;
  linkedToTopic: number;
  alreadyLinked: number;
  skippedRows: number;
  errors: string[];
}

export interface WorkbookQuestionImportResult {
  subjectId: number;
  subjectCode: string;
  subjectTitle: string;
  subjectCreated: boolean;
  totalRows: number;
  processedRows: number;
  createdQuestions: number;
  reusedQuestions: number;
  linkedToTopic: number;
  alreadyLinked: number;
  skippedRows: number;
  topics: WorkbookTopicImportResult[];
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
