import { Component, Input } from '@angular/core';

import { TopicNode, TopicQuestion, TopicQuestionOption } from '../../models/learning.models';

@Component({
  selector: 'app-topic-quiz',
  standalone: true,
  templateUrl: './topic-quiz.component.html',
  styleUrl: './topic-quiz.component.scss',
})
export class TopicQuizComponent {
  @Input() topic: TopicNode | null = null;

  protected selectedAnswers: Record<string, string> = {};

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
    return !!question.requiresSubscription;
  }

  protected hasQuestions(): boolean {
    return !!this.topic?.questions?.length;
  }
}
