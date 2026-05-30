import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  ExamEvaluationResponse,
  ExamQuestion,
  ExamQuestionOption,
  TopicNode,
} from '../../models/learning.models';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LearningDataService } from '../../services/learning-data.service';
import { SubscriptionAccessService } from '../../services/subscription-access.service';

@Component({
  selector: 'app-realtime-exam-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './realtime-exam-page.component.html',
  styleUrl: './realtime-exam-page.component.scss',
})
export class RealtimeExamPageComponent implements OnDestroy {
  protected readonly totalSeconds = 60 * 60; // 60 minutes
  protected timeLeftSeconds = this.totalSeconds;
  protected showSubjectSelector = false; // New state for subject selection
  protected isExamStarted = false;
  protected isExamFinished = false;
  protected activeQuestionIndex = 0;
  protected selectedAnswers: Record<string, string> = {};
  protected questions: ExamQuestion[] = [];
  protected availableSubjects: TopicNode[] = [];
  protected selectedSubjectCodes: Set<string> = new Set();
  protected loadingSubjects = false;
  protected loadingQuestions = false;
  protected loadingEvaluation = false;
  protected loadError = '';
  protected score = 0;
  protected scorePercent = 0;
  protected answerChecks: Record<string, boolean> = {};
  protected answerExplanations: Record<string, string> = {};
  protected correctAnswerTexts: Record<string, string> = {};
  protected answerImageUrls: Record<string, string> = {};

  private timerHandle: ReturnType<typeof setInterval> | null = null;

  constructor(
    private readonly learningDataService: LearningDataService,
    private readonly authService: AuthService,
    private readonly subscriptionAccessService: SubscriptionAccessService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef,
  ) {
    console.log('[RealtimeExamPageComponent] Component initialized');
  }

  protected hasPremiumAccess(): boolean {
    return this.subscriptionAccessService.hasActiveSubscription();
  }

  protected goToSubscription(): void {
    this.subscriptionAccessService.redirectToSubscription(this.router, '/learning/realtime-exam');
  }

  protected async openSubjectSelector(): Promise<void> {
    if (!this.hasPremiumAccess()) {
      this.loadError = 'Real-time quiz is available only for premium users.';
      return;
    }

    console.log('[RealtimeExamPageComponent] Opening subject selector...');
    this.loadingSubjects = true;
    this.loadError = '';
    
    try {
      const subjects = await this.learningDataService.getSubjects();
      if (!subjects) {
        this.loadError = 'Session expired. Please login again.';
        this.authService.logout();
        this.availableSubjects = [];
        return;
      }

      console.log('[RealtimeExamPageComponent] Subjects loaded:', subjects.length);
      this.availableSubjects = subjects;
      this.showSubjectSelector = true;
    } catch (err) {
      console.error('[RealtimeExamPageComponent] Error loading subjects:', err);
      this.loadError = 'Session expired. Please login again.';
      this.authService.logout();
    } finally {
      this.loadingSubjects = false;
      this.cdr.detectChanges();
    }
  }

  protected closeSubjectSelector(): void {
    console.log('[RealtimeExamPageComponent] Closing subject selector');
    this.showSubjectSelector = false;
    this.selectedSubjectCodes.clear();
    this.cdr.detectChanges();
  }

  protected toggleSubject(subjectId: string): void {
    if (this.selectedSubjectCodes.has(subjectId)) {
      this.selectedSubjectCodes.delete(subjectId);
    } else {
      this.selectedSubjectCodes.add(subjectId);
    }
  }

  protected selectAllSubjects(): void {
    this.selectedSubjectCodes = new Set(this.availableSubjects.map(s => s.id));
  }

  protected clearAllSubjects(): void {
    this.selectedSubjectCodes.clear();
  }

  protected async startExam(): Promise<void> {
    if (!this.hasPremiumAccess()) {
      this.loadError = 'Real-time quiz is available only for premium users.';
      return;
    }

    if (this.selectedSubjectCodes.size === 0) {
      this.loadError = 'Please select at least one subject.';
      return;
    }

    this.loadingQuestions = true;
    this.loadError = '';

    try {
      const subjectCodes = Array.from(this.selectedSubjectCodes);
      console.log('[RealtimeExamPageComponent] Fetching exam questions for subjects:', subjectCodes);
      const questionsData = await this.learningDataService.getExamQuestions(subjectCodes);

      console.log('[RealtimeExamPageComponent] Questions fetched:', questionsData?.length, questionsData);

      if (!questionsData || questionsData.length === 0) {
        this.loadError = 'No questions found for selected subjects.';
        this.loadingQuestions = false;
        this.cdr.detectChanges();
        return;
      }

      this.questions = questionsData;

      console.log('[RealtimeExamPageComponent] Questions loaded, count:', this.questions.length);

      this.showSubjectSelector = false;
      this.isExamStarted = true;
      this.isExamFinished = false;
      this.activeQuestionIndex = 0;
      this.selectedAnswers = {};
      this.score = 0;
      this.scorePercent = 0;
      this.answerChecks = {};
      this.answerExplanations = {};
      this.correctAnswerTexts = {};
      this.answerImageUrls = {};
      this.timeLeftSeconds = this.totalSeconds;
      this.startTimer();
    } catch (err) {
      console.error('Failed to load exam questions:', err);
      if (err instanceof Error && err.message === 'EXAM_PREMIUM_FORBIDDEN') {
        this.loadError = 'Premium subscription is required to start real-time quiz.';
        return;
      }

      this.loadError = 'Failed to load exam questions. Please try again.';
    } finally {
      this.loadingQuestions = false;
      this.cdr.detectChanges();
    }
  }

  protected selectAnswer(questionId: string, optionId: string): void {
    this.selectedAnswers[questionId] = optionId;
  }

  protected getSelectedOption(question: ExamQuestion): ExamQuestionOption | undefined {
    const selectedId = this.selectedAnswers[question.id];
    return question.options?.find(o => o.id === selectedId);
  }

  protected isAnswerCorrect(questionId: string): boolean | undefined {
    if (!(questionId in this.answerChecks)) {
      return undefined;
    }

    return this.answerChecks[questionId];
  }

  protected getExplanation(questionId: string): string {
    return this.answerExplanations[questionId] || '';
  }

  protected getCorrectAnswerText(questionId: string): string {
    return this.correctAnswerTexts[questionId] || '';
  }

  protected getAnswerImageUrl(questionId: string): string {
    return this.answerImageUrls[questionId] || '';
  }

  protected goToQuestion(index: number): void {
    this.activeQuestionIndex = index;
  }

  protected prevQuestion(): void {
    if (this.activeQuestionIndex > 0) {
      this.activeQuestionIndex--;
    }
  }

  protected nextQuestion(): void {
    if (this.activeQuestionIndex < this.questions.length - 1) {
      this.activeQuestionIndex++;
    }
  }

  protected answeredCount(): number {
    return Object.keys(this.selectedAnswers).length;
  }

  protected isAnswered(questionId: string): boolean {
    return !!this.selectedAnswers[questionId];
  }

  protected scoreCount(): number {
    return this.score;
  }

  protected passPercent(): number {
    return this.scorePercent;
  }

  protected hasPassed(): boolean {
    return this.passPercent() >= 50;
  }

  protected formattedTimeLeft(): string {
    const minutes = Math.floor(this.timeLeftSeconds / 60);
    const seconds = this.timeLeftSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  protected async submitExam(): Promise<void> {
    await this.evaluateAnswers(true);
    this.finishExam();
  }

  protected restartExam(): void {
    console.log('[RealtimeExamPageComponent] Restarting exam');
    this.showSubjectSelector = false;
    this.isExamStarted = false;
    this.isExamFinished = false;
    this.selectedAnswers = {};
    this.questions = [];
    this.score = 0;
    this.scorePercent = 0;
    this.answerChecks = {};
    this.answerExplanations = {};
    this.correctAnswerTexts = {};
    this.answerImageUrls = {};
    this.selectedSubjectCodes.clear();
    this.openSubjectSelector();
  }

  private startTimer(): void {
    this.timerHandle = setInterval(() => {
      this.timeLeftSeconds--;

      if (this.timeLeftSeconds <= 0) {
        void this.submitExam();
      }

      this.cdr.detectChanges();
    }, 1000);
  }

  private finishExam(): void {
    this.clearTimer();
    this.isExamStarted = false;
    this.isExamFinished = true;
    this.cdr.detectChanges();
  }

  private async evaluateAnswers(includeExplanations: boolean): Promise<void> {
    if (!this.hasPremiumAccess()) {
      this.loadError = 'Premium subscription is required to evaluate real-time quiz.';
      return;
    }

    if (Object.keys(this.selectedAnswers).length === 0) {
      this.score = 0;
      this.scorePercent = 0;
      return;
    }

    this.loadingEvaluation = true;

    try {
      const evaluation = await this.learningDataService.evaluateExamAnswers(
        this.questions.map((question) => question.id),
        this.selectedAnswers,
        this.questions.length,
        includeExplanations,
      );

      if (!evaluation) {
        return;
      }

      this.applyEvaluation(evaluation, includeExplanations);
    } catch (err) {
      if (err instanceof Error && err.message === 'EXAM_PREMIUM_FORBIDDEN') {
        this.loadError = 'Premium subscription is required for real-time quiz.';
      }
    } finally {
      this.loadingEvaluation = false;
      this.cdr.detectChanges();
    }
  }

  private applyEvaluation(evaluation: ExamEvaluationResponse, includeExplanations: boolean): void {
    this.score = evaluation.score;
    this.scorePercent = evaluation.percent;

    this.answerChecks = {};
    this.correctAnswerTexts = {};
    this.answerImageUrls = {};
    for (const item of evaluation.results) {
      this.answerChecks[item.questionId] = item.isCorrect;
      this.correctAnswerTexts[item.questionId] = item.correctOptionText || '';
      this.answerImageUrls[item.questionId] = item.answerImageUrl || '';
      if (includeExplanations) {
        this.answerExplanations[item.questionId] = item.explanation || '';
      }
    }
  }

  private clearTimer(): void {
    if (this.timerHandle) {
      clearInterval(this.timerHandle);
      this.timerHandle = null;
    }
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }
}
