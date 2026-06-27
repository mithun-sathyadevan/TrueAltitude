import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Router } from '@angular/router';

import { TopicNode, TopicQuestion, TopicQuestionOption } from '../../models/learning.models';
import { SubscriptionAccessService } from '../../services/subscription-access.service';

export interface TopicQuizCompletionEvent {
  topicId: string;
  scorePercent: number;
}

@Component({
  selector: 'app-topic-quiz',
  standalone: true,
  templateUrl: './topic-quiz.component.html',
  styleUrl: './topic-quiz.component.scss',
})
export class TopicQuizComponent {
  @Input() topic: TopicNode | null = null;
  @Input() loadingQuestions = false;
  @Input() loadError = '';
  @Input() showNextTopicNavigation = false;
  @Input() hasNextTopic = false;
  @Input() nextTopicTitle: string | null = null;
  @Output() nextTopicRequested = new EventEmitter<void>();
  @Output() quizCompleted = new EventEmitter<TopicQuizCompletionEvent>();

  protected selectedAnswers: Record<string, string> = {};
  protected revealedAnswers: Record<string, boolean> = {};
  protected isFinished = false;
  protected showScorePopup = false;
  protected celebrationParticles: Array<{ left: string; top: string; delay: string; duration: string; size: string; color: string; rotate: string }> = [];

  constructor(
    private readonly subscriptionAccessService: SubscriptionAccessService,
    private readonly router: Router,
  ) {}

  protected onSelectOption(questionId: string, optionId: string): void {
    if (this.isFinished) {
      return;
    }

    this.selectedAnswers = {
      ...this.selectedAnswers,
      [questionId]: optionId,
    };
  }

  protected onRevealAnswer(questionId: string): void {
    this.revealedAnswers = {
      ...this.revealedAnswers,
      [questionId]: true,
    };
  }

  protected isAnswerRevealed(questionId: string): boolean {
    return !!this.revealedAnswers[questionId];
  }

  protected isOptionSelected(questionId: string, optionId: string): boolean {
    return this.selectedAnswers[questionId] === optionId;
  }

  protected getSelectedOption(question: TopicQuestion): TopicQuestionOption | undefined {
    const selectedOptionId = this.selectedAnswers[question.id];
    return question.options.find((option) => option.id === selectedOptionId);
  }

  protected getAnsweredQuestions(): TopicQuestion[] {
    return (this.topic?.questions || []).filter((question) => !!this.selectedAnswers[question.id]);
  }

  protected getUnlockedQuestions(): TopicQuestion[] {
    return (this.topic?.questions || []).filter((question) => !this.isQuestionLocked(question));
  }

  protected getAnsweredCount(): number {
    return this.getAnsweredQuestions().filter((question) => !this.isQuestionLocked(question)).length;
  }

  protected getCorrectAnswersCount(): number {
    return this.getUnlockedQuestions().filter((question) => {
      const selectedOption = this.getSelectedOption(question);
      return !!selectedOption?.isCorrect;
    }).length;
  }

  protected getTotalScorableQuestions(): number {
    return this.getUnlockedQuestions().length;
  }

  protected getScorePercentage(): number {
    const total = this.getTotalScorableQuestions();
    if (total === 0) {
      return 0;
    }

    return Math.round((this.getCorrectAnswersCount() / total) * 100);
  }

  protected onFinishQuiz(): void {
    this.isFinished = true;
    this.showScorePopup = true;
    this.celebrationParticles = this.getCelebrationParticles();

    if (this.topic?.id) {
      this.quizCompleted.emit({
        topicId: this.topic.id,
        scorePercent: this.getScorePercentage(),
      });
    }
  }

  protected onRetakeQuiz(): void {
    this.selectedAnswers = {};
    this.revealedAnswers = {};
    this.isFinished = false;
    this.showScorePopup = false;
    this.celebrationParticles = [];
  }

  protected closeScorePopup(): void {
    this.showScorePopup = false;
  }

  protected canFinishQuiz(): boolean {
    const total = this.getTotalScorableQuestions();
    return total > 0 && this.getAnsweredCount() === total && !this.isFinished;
  }

  protected hasFinishedQuiz(): boolean {
    return this.isFinished;
  }

  protected hasCelebration(): boolean {
    return this.hasFinishedQuiz() && this.getScorePercentage() > 75;
  }

  protected getCelebrationParticles(): Array<{ left: string; top: string; delay: string; duration: string; size: string; color: string; rotate: string }> {
    if (!this.hasCelebration()) {
      return [];
    }

    const colors = ['#facc15', '#fb7185', '#60a5fa', '#34d399', '#f97316', '#a78bfa'];

    return Array.from({ length: 24 }, (_, index) => {
      const side = index % 2 === 0 ? 'left' : 'right';
      const spread = 8 + Math.floor(Math.random() * 84);

      return {
        left: `${10 + Math.floor(Math.random() * 80)}%`,
        top: `${8 + Math.floor(Math.random() * 18)}%`,
        delay: `${Math.random() * 0.35}s`,
        duration: `${0.9 + Math.random() * 0.7}s`,
        size: `${6 + Math.floor(Math.random() * 5)}px`,
        color: colors[index % colors.length],
        rotate: `${side === 'left' ? -spread : spread}deg`,
      };
    });
  }

  protected getCorrectOption(question: TopicQuestion): TopicQuestionOption | undefined {
    return question.options.find((option) => option.isCorrect);
  }

  protected getQuestionExplanation(question: TopicQuestion): string {
    return question.explanation || this.getCorrectOption(question)?.explanation || '';
  }

  protected getQuestionReviewText(question: TopicQuestion): string {
    return this.getQuestionExplanation(question);
  }

  protected getCorrectAnswerText(question: TopicQuestion): string {
    return this.getCorrectOption(question)?.text || '';
  }

  protected getCorrectAnswerExplanation(question: TopicQuestion): string {
    return question.explanation || this.getCorrectOption(question)?.explanation || '';
  }

  protected isQuestionLocked(question: TopicQuestion): boolean {
    return !!question.requiresSubscription && !this.subscriptionAccessService.hasActiveSubscription();
  }

  protected isTopicLocked(): boolean {
    return !!this.topic?.requiresSubscription && !this.subscriptionAccessService.hasActiveSubscription();
  }

  protected goToSubscription(): void {
    this.subscriptionAccessService.redirectToSubscription(this.router, this.router.url);
  }

  protected hasQuestions(): boolean {
    return !!this.topic?.questions?.length;
  }

  protected onNextTopic(): void {
    if (!this.hasNextTopic) {
      return;
    }

    this.nextTopicRequested.emit();
  }
}
