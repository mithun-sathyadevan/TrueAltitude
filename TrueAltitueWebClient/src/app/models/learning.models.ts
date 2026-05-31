export interface TopicQuestionOption {
  id: string;
  text: string;
  isCorrect: boolean;
  explanation?: string;
}

export interface TopicQuestion {
  id: string;
  text: string;
  answerImageUrl?: string;
  explanation: string;
  requiresSubscription?: boolean;
  subscriptionLabel?: string;
  options: TopicQuestionOption[];
}

export interface TopicVideo {
  id: string;
  title: string;
  duration: string;
  summary: string;
}

export interface TopicBlog {
  id: string;
  chapter: string;
  title: string;
  readMinutes: number;
  summary: string;
}

export interface TopicNode {
  id: string;
  title: string;
  description: string;
  questionCount?: number;
  requiresSubscription?: boolean;
  subscriptionLabel?: string;
  children?: TopicNode[];
  questions?: TopicQuestion[];
  videos?: TopicVideo[];
  blogs?: TopicBlog[];
}

export interface ExamQuestionOption {
  id: string;
  text: string;
}

export interface ExamQuestion {
  id: string;
  text: string;
  options: ExamQuestionOption[];
}

export interface ExamEvaluationItem {
  questionId: string;
  isCorrect: boolean;
  isAnswered: boolean;
  correctOptionId: string;
  correctOptionText: string;
  explanation: string;
  answerImageUrl?: string;
}

export interface ExamEvaluationResponse {
  score: number;
  totalQuestions: number;
  percent: number;
  results: ExamEvaluationItem[];
}
