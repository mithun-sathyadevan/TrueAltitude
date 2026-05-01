import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ChapterBlogsComponent } from '../../components/chapter-blogs/chapter-blogs.component';
import { TopicQuizComponent } from '../../components/topic-quiz/topic-quiz.component';
import { TopicTreeComponent } from '../../components/topic-tree/topic-tree.component';
import { VideoCourseComponent } from '../../components/video-course/video-course.component';
import { TopicNode } from '../../models/learning.models';
import { LearningDataService } from '../../services/learning-data.service';
import { SubscriptionAccessService } from '../../services/subscription-access.service';

@Component({
  selector: 'app-learning-page',
  standalone: true,
  imports: [CommonModule, RouterLink, TopicTreeComponent, TopicQuizComponent, VideoCourseComponent, ChapterBlogsComponent],
  templateUrl: './learning-page.component.html',
  styleUrl: './learning-page.component.scss',
})
export class LearningPageComponent {
  protected selectedSubject: TopicNode | null = null;
  protected selectedTopic: TopicNode | null = null;
  protected isTopicPickerOpen = false;
  protected loading = true;
  protected loadError = '';

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly router: Router,
    private readonly learningDataService: LearningDataService,
    private readonly subscriptionAccessService: SubscriptionAccessService,
    private readonly cdr: ChangeDetectorRef,
  ) {
    this.activatedRoute.paramMap.subscribe((params) => {
      void this.loadSubject(params.get('subjectId'));
    });
  }

  private async loadSubject(subjectId: string | null): Promise<void> {
    if (!subjectId) {
      void this.router.navigate(['/learning/subjects']);
      return;
    }

    this.loading = true;
    this.loadError = '';

    try {
      const subject = await this.learningDataService.getSubjectById(subjectId);
      if (!subject) {
        this.loadError = 'Could not load subject from database.';
        return;
      }

      if (subject.requiresSubscription && !this.hasPremiumAccess()) {
        this.subscriptionAccessService.redirectToSubscription(this.router, `/learning/topics/${subject.id}`);
        return;
      }

      this.selectedSubject = subject;
      this.selectedTopic = this.learningDataService.findInitialTopic(subject.children || [], subject);
      this.cdr.detectChanges();
    } finally {
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  protected onTopicSelected(topic: TopicNode): void {
    if (!this.selectedSubject) {
      return;
    }

    if (this.isTopicLocked(topic)) {
      this.subscriptionAccessService.redirectToSubscription(this.router, `/learning/topics/${this.selectedSubject.id}`);
      return;
    }

    this.selectedTopic = topic;
    this.closeTopicPicker();
  }

  protected hasPremiumAccess(): boolean {
    return this.subscriptionAccessService.hasActiveSubscription();
  }

  protected isTopicLocked(topic: TopicNode): boolean {
    return !!topic.requiresSubscription && !this.hasPremiumAccess();
  }

  protected isTopicGroup(topic: TopicNode): boolean {
    return !!topic.children?.length && !topic.questions?.length;
  }

  protected topicQuestionCount(topic: TopicNode): number {
    return topic.questions?.length || 0;
  }

  protected topicTotalQuestionCount(topic: TopicNode): number {
    let total = topic.questions?.length || 0;
    for (const child of topic.children || []) {
      total += this.topicTotalQuestionCount(child);
    }
    return total;
  }

  protected selectChildTopic(topic: TopicNode): void {
    if (!this.selectedSubject) {
      return;
    }

    if (this.isTopicLocked(topic)) {
      this.subscriptionAccessService.redirectToSubscription(this.router, `/learning/topics/${this.selectedSubject.id}`);
      return;
    }

    this.selectedTopic = topic;
  }

  protected openTopicPicker(): void {
    this.isTopicPickerOpen = true;
  }

  protected closeTopicPicker(): void {
    this.isTopicPickerOpen = false;
  }
}
