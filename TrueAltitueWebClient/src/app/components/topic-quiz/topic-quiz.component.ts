import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';

import { TopicNode, TopicQuestion, TopicQuestionOption } from '../../models/learning.models';
import { SubscriptionAccessService } from '../../services/subscription-access.service';

@Component({
  selector: 'app-topic-quiz',
  standalone: true,
  templateUrl: './topic-quiz.component.html',
  styleUrl: './topic-quiz.component.scss',
})
export class TopicQuizComponent {
  @Input() topic: TopicNode | null = null;

  protected selectedAnswers: Record<string, string> = {};

  constructor(
    private readonly subscriptionAccessService: SubscriptionAccessService,
    private readonly router: Router,
  ) {}

  protected onSelectOption(questionId: string, optionId: string): void {
    this.selectedAnswers = {
      ...this.selectedAnswers,
      [questionId]: optionId,
    };
  }

  protected isOptionSelected(questionId: string, optionId: string): boolean {
    return this.selectedAnswers[questionId] === optionId;
  }

  protected getSelectedOption(question: TopicQuestion): TopicQuestionOption | undefined {
    const selectedOptionId = this.selectedAnswers[question.id];
    return question.options.find((option) => option.id === selectedOptionId);
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
}
