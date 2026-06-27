import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import { environment } from '../../environments/environment';

import {
  ExamConfig,
  ExamEvaluationResponse,
  ExamQuestion,
  TopicCompletionPayload,
  TopicNode,
  TopicPerformanceInsight,
  TopicProgressSummary,
  TopicQuestion,
} from '../models/learning.models';

@Injectable({ providedIn: 'root' })
export class LearningDataService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/learning`;
  private readonly requestTimeoutMs = 10000;
  private readonly requestRetryDelayMs = 400;
  private readonly maxRequestAttempts = 2;
  private subjectListCache: TopicNode[] | null = null;
  private readonly subjectDetailCache = new Map<string, TopicNode>();

  constructor(private readonly http: HttpClient) {}

  async getSubjects(): Promise<TopicNode[] | undefined> {
    if (this.subjectListCache !== null) {
      return this.deepClone(this.subjectListCache);
    }

    const subjects = await this.getWithRetry<TopicNode[]>(`${this.apiUrl}/subjects`);
    if (!subjects) {
      return undefined;
    }

    this.subjectListCache = subjects.map((subject) => this.normalizeTopic(subject));
    return this.deepClone(this.subjectListCache);
  }

  async getSubjectById(subjectId: string): Promise<TopicNode | undefined> {
    const subject = await this.getWithRetry<TopicNode>(`${this.apiUrl}/subjects/${encodeURIComponent(subjectId)}`);
    if (!subject) {
      return undefined;
    }

    const normalized = this.normalizeTopic(subject);
    this.subjectDetailCache.set(subjectId, normalized);
    return this.deepClone(normalized);
  }

  async getTopicQuestions(topicId: string): Promise<TopicQuestion[] | undefined> {
    try {
      const result = await firstValueFrom(
        this.http
          .get<TopicQuestion[]>(`${this.apiUrl}/topics/${encodeURIComponent(topicId)}/questions`)
          .pipe(timeout(this.requestTimeoutMs)),
      );

      return result.map((question) => {
        const rawQuestion = question as any;
        return {
          ...question,
          explanation: question.explanation || rawQuestion.explanationText || '',
          answerImageUrl: question.answerImageUrl || rawQuestion.answerImageURL || '',
          options: this.shuffleArray(question.options || []),
        };
      });
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 403) {
        throw new Error('TOPIC_PREMIUM_FORBIDDEN');
      }

      console.error('[LearningDataService] getTopicQuestions failed:', err);
      return undefined;
    }
  }

  findInitialTopic(topics: TopicNode[], fallback: TopicNode): TopicNode {
    const firstTopic = topics[0];
    if (!firstTopic) {
      return fallback;
    }

    return firstTopic;
  }

  async getExamQuestions(subjectCodes: string[]): Promise<ExamQuestion[] | undefined> {
    try {
      console.log('[LearningDataService] getExamQuestions - requesting:', { subjectCodes });
      const result = await firstValueFrom(
        this.http
          .post<ExamQuestion[]>(
            `${this.apiUrl}/exam/questions`,
            { subjectCodes },
          )
          .pipe(timeout(this.requestTimeoutMs)),
      );
      console.log('[LearningDataService] getExamQuestions - response:', result);
      return (result || []).map((question) => ({
        ...question,
        options: this.shuffleArray(question.options || []),
      }));
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 403) {
        throw new Error('EXAM_PREMIUM_FORBIDDEN');
      }

      console.error('[LearningDataService] getExamQuestions failed:', err);
      return undefined;
    }
  }

  async getExamConfig(): Promise<ExamConfig | undefined> {
    try {
      return await firstValueFrom(
        this.http
          .get<ExamConfig>(`${this.apiUrl}/exam/config`)
          .pipe(timeout(this.requestTimeoutMs)),
      );
    } catch (err) {
      console.error('[LearningDataService] getExamConfig failed:', err);
      return undefined;
    }
  }

  async getTopicProgressSummary(): Promise<TopicProgressSummary | undefined> {
    try {
      const cacheBust = Date.now();
      return await firstValueFrom(
        this.http
          .get<TopicProgressSummary>(`${this.apiUrl}/progress/summary?t=${cacheBust}`)
          .pipe(timeout(this.requestTimeoutMs)),
      );
    } catch (err) {
      console.error('[LearningDataService] getTopicProgressSummary failed:', err);
      return undefined;
    }
  }

  async getTopicPerformanceInsight(subjectCode?: string): Promise<TopicPerformanceInsight | undefined> {
    try {
      const cacheBust = Date.now();
      const normalizedSubjectCode = (subjectCode || '').trim();
      const subjectParam = normalizedSubjectCode ? `&subjectCode=${encodeURIComponent(normalizedSubjectCode)}` : '';
      return await firstValueFrom(
        this.http
          .get<TopicPerformanceInsight>(`${this.apiUrl}/progress/insights?t=${cacheBust}${subjectParam}`)
          .pipe(timeout(this.requestTimeoutMs)),
      );
    } catch (err) {
      console.error('[LearningDataService] getTopicPerformanceInsight failed:', err);
      return undefined;
    }
  }

  async getCompletedTopicCodes(): Promise<string[] | undefined> {
    try {
      const cacheBust = Date.now();
      return await firstValueFrom(
        this.http
          .get<string[]>(`${this.apiUrl}/progress/topics/completed?t=${cacheBust}`)
          .pipe(timeout(this.requestTimeoutMs)),
      );
    } catch (err) {
      console.error('[LearningDataService] getCompletedTopicCodes failed:', err);
      return undefined;
    }
  }

  async markTopicCompleted(topicId: string, scorePercent?: number): Promise<boolean> {
    const normalizedTopicId = (topicId || '').trim();
    if (!normalizedTopicId) {
      return false;
    }

    const payload: TopicCompletionPayload = {
      topicCode: normalizedTopicId,
    };

    if (typeof scorePercent === 'number' && Number.isFinite(scorePercent)) {
      payload.scorePercent = Math.max(0, Math.min(100, Math.round(scorePercent)));
    }

    let lastError: unknown;

    for (let attempt = 1; attempt <= this.maxRequestAttempts; attempt++) {
      try {
        await firstValueFrom(
          this.http
            .post(`${this.apiUrl}/progress/topics/complete`, payload)
            .pipe(timeout(this.requestTimeoutMs)),
        );

        return true;
      } catch (err) {
        lastError = err;
        const canRetry = attempt < this.maxRequestAttempts && this.isRetryable(err);
        if (!canRetry) {
          break;
        }

        await this.delay(this.requestRetryDelayMs);
      }
    }

    console.error('[LearningDataService] markTopicCompleted failed:', lastError);
    return false;
  }

  async evaluateExamAnswers(
    questionIds: string[],
    selectedAnswers: Record<string, string>,
    totalQuestions: number,
    includeExplanations: boolean,
  ): Promise<ExamEvaluationResponse | undefined> {
    const questions = questionIds.map((questionId) => ({ questionId }));
    const answers = Object.entries(selectedAnswers).map(([questionId, optionId]) => ({ questionId, optionId }));

    try {
      return await firstValueFrom(
        this.http
          .post<ExamEvaluationResponse>(
            `${this.apiUrl}/exam/evaluate`,
            { questions, answers, totalQuestions, includeExplanations },
          )
          .pipe(timeout(this.requestTimeoutMs)),
      );
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 403) {
        throw new Error('EXAM_PREMIUM_FORBIDDEN');
      }

      console.error('[LearningDataService] evaluateExamAnswers failed:', err);
      return undefined;
    }
  }

  private async getWithRetry<T>(url: string): Promise<T | undefined> {
    let lastError: unknown;

    for (let attempt = 1; attempt <= this.maxRequestAttempts; attempt++) {
      try {
        return await firstValueFrom(
          this.http
            .get<T>(url)
            .pipe(timeout(this.requestTimeoutMs)),
        );
      } catch (err) {
        lastError = err;
        const canRetry = attempt < this.maxRequestAttempts && this.isRetryable(err);
        if (!canRetry) {
          break;
        }

        await this.delay(this.requestRetryDelayMs);
      }
    }

    console.error('[LearningDataService] request failed:', url, lastError);
    return undefined;
  }

  private isRetryable(err: unknown): boolean {
    if (err instanceof HttpErrorResponse) {
      return err.status === 0 || err.status === 401 || err.status === 502 || err.status === 503 || err.status === 504;
    }

    if (err instanceof Error && err.name === 'TimeoutError') {
      return true;
    }

    return false;
  }

  private async delay(ms: number): Promise<void> {
    await new Promise<void>((resolve) => {
      setTimeout(resolve, ms);
    });
  }

  private normalizeTopic(topic: TopicNode): TopicNode {
    return {
      ...topic,
      children: (topic.children || []).map((child) => this.normalizeTopic(child)),
      questionCount: topic.questionCount ?? topic.questions?.length ?? 0,
      questions: (topic.questions || []).map((question) => {
        const rawQuestion = question as any;
        return {
          ...question,
          explanation: question.explanation || rawQuestion.explanationText || '',
          answerImageUrl: question.answerImageUrl || rawQuestion.answerImageURL || '',
          options: this.shuffleArray(question.options || []),
        };
      }),
      videos: topic.videos || [],
      blogs: topic.blogs || [],
    };
  }

  private deepClone<T>(value: T): T {
    return JSON.parse(JSON.stringify(value)) as T;
  }

  private shuffleArray<T>(items: T[]): T[] {
    const copy = [...items];

    for (let i = copy.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [copy[i], copy[j]] = [copy[j], copy[i]];
    }

    return copy;
  }
}
