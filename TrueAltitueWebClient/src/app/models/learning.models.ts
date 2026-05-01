export interface TopicQuestionOption {
  id: string;
  text: string;
  isCorrect: boolean;
  explanation: string;
}

export interface TopicQuestion {
  id: string;
  text: string;
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
  requiresSubscription?: boolean;
  subscriptionLabel?: string;
  children?: TopicNode[];
  questions?: TopicQuestion[];
  videos?: TopicVideo[];
  blogs?: TopicBlog[];
}
