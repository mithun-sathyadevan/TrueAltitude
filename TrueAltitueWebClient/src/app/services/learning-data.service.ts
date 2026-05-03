import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';

import { ExamEvaluationResponse, ExamQuestion, TopicNode } from '../models/learning.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class LearningDataService {
  private readonly apiUrl = 'http://localhost:5137/api/learning';
  private readonly requestTimeoutMs = 10000;
  private readonly requestRetryDelayMs = 400;
  private readonly maxRequestAttempts = 2;
  private subjectListCache: TopicNode[] | null = null;
  private readonly subjectDetailCache = new Map<string, TopicNode>();

  constructor(
    private readonly http: HttpClient,
    private readonly authService: AuthService,
  ) {}

  async getSubjects(): Promise<TopicNode[]> {
    if (this.subjectListCache !== null) {
      return this.deepClone(this.subjectListCache);
    }

    const subjects = await this.getWithRetry<TopicNode[]>(`${this.apiUrl}/subjects`);
    if (!subjects) {
      return [];
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

  findInitialTopic(topics: TopicNode[], fallback: TopicNode): TopicNode {
    const firstTopic = topics[0];
    if (!firstTopic) {
      return fallback;
    }

    return firstTopic;
  }

  async getExamQuestions(subjectCodes: string[]): Promise<ExamQuestion[] | undefined> {
    const headers = this.createAuthHeaders();

    try {
      console.log('[LearningDataService] getExamQuestions - requesting:', { subjectCodes, count: 10 });
      const result = await firstValueFrom(
        this.http
          .post<ExamQuestion[]>(
            `${this.apiUrl}/exam/questions`,
            { subjectCodes, count: 10 },
            { headers },
          )
          .pipe(timeout(this.requestTimeoutMs)),
      );
      console.log('[LearningDataService] getExamQuestions - response:', result);
      return result;
    } catch (err) {
      console.error('[LearningDataService] getExamQuestions failed:', err);
      return undefined;
    }
  }

  async evaluateExamAnswers(
    questionIds: string[],
    selectedAnswers: Record<string, string>,
    totalQuestions: number,
    includeExplanations: boolean,
  ): Promise<ExamEvaluationResponse | undefined> {
    const headers = this.createAuthHeaders();
    const questions = questionIds.map((questionId) => ({ questionId }));
    const answers = Object.entries(selectedAnswers).map(([questionId, optionId]) => ({ questionId, optionId }));

    try {
      return await firstValueFrom(
        this.http
          .post<ExamEvaluationResponse>(
            `${this.apiUrl}/exam/evaluate`,
            { questions, answers, totalQuestions, includeExplanations },
            { headers },
          )
          .pipe(timeout(this.requestTimeoutMs)),
      );
    } catch (err) {
      console.error('[LearningDataService] evaluateExamAnswers failed:', err);
      return undefined;
    }
  }

  private createAuthHeaders(): HttpHeaders | undefined {
    const token = this.authService.getAuthToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : undefined;
  }

  private async getWithRetry<T>(url: string): Promise<T | undefined> {
    let lastError: unknown;

    for (let attempt = 1; attempt <= this.maxRequestAttempts; attempt++) {
      const headers = this.createAuthHeaders();

      try {
        return await firstValueFrom(
          this.http
            .get<T>(url, { headers })
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
      questions: (topic.questions || []).map((question) => {
        const rawQuestion = question as any;
        return {
          ...question,
          explanation: question.explanation || rawQuestion.explanationText || '',
          options: question.options || [],
        };
      }),
      videos: topic.videos || [],
      blogs: topic.blogs || [],
    };
  }

  private deepClone<T>(value: T): T {
    return JSON.parse(JSON.stringify(value)) as T;
  }
}
