import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ChapterBlogsComponent } from '../../components/chapter-blogs/chapter-blogs.component';
import { TopicQuizComponent, TopicQuizCompletionEvent } from '../../components/topic-quiz/topic-quiz.component';
import { TopicTreeComponent } from '../../components/topic-tree/topic-tree.component';
import { VideoCourseComponent } from '../../components/video-course/video-course.component';
import { TopicNode } from '../../models/learning.models';
import { LearningDataService } from '../../services/learning-data.service';
import { ProgressRefreshService } from '../../services/progress-refresh.service';
import { SubscriptionAccessService } from '../../services/subscription-access.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-learning-page',
  standalone: true,
  imports: [CommonModule, RouterLink, TopicTreeComponent, TopicQuizComponent, VideoCourseComponent, ChapterBlogsComponent],
  templateUrl: './learning-page.component.html',
  styleUrl: './learning-page.component.scss',
})
export class LearningPageComponent implements OnDestroy {
  protected selectedSubject: TopicNode | null = null;
  protected selectedTopic: TopicNode | null = null;
  protected completedTopicIds: string[] = [];
  protected isTopicPickerOpen = false;
  protected loading = true;
  protected loadingTopicQuestions = false;
  protected topicQuestionLoadError = '';
  protected topicProgressSaveError = '';
  protected loadError = '';
  protected readonly showLearningResources = environment.features.showLearningVideoCourse || environment.features.showLearningChapterBlogs;
  protected readonly showLearningVideoCourse = environment.features.showLearningVideoCourse;
  protected readonly showLearningChapterBlogs = environment.features.showLearningChapterBlogs;

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly router: Router,
    private readonly learningDataService: LearningDataService,
    private readonly progressRefreshService: ProgressRefreshService,
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
      this.completedTopicIds = await this.getCompletedTopicIds();
      this.selectedTopic = this.getInitialTopic(subject);
      this.topicQuestionLoadError = '';

      if (this.selectedTopic) {
        await this.ensureTopicQuestionsLoaded(this.selectedTopic);
      }

      this.cdr.detectChanges();
    } finally {
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  protected async onTopicSelected(topic: TopicNode): Promise<void> {
    if (!this.selectedSubject) {
      return;
    }

    this.selectedTopic = topic;
    this.topicQuestionLoadError = '';
    await this.ensureTopicQuestionsLoaded(topic);
    this.closeTopicPicker();
  }

  protected hasPremiumAccess(): boolean {
    return this.subscriptionAccessService.hasActiveSubscription();
  }

  protected isTopicLocked(topic: TopicNode): boolean {
    return !!topic.requiresSubscription && !this.hasPremiumAccess();
  }

  protected isTopicGroup(topic: TopicNode): boolean {
    return !!topic.children?.length;
  }

  protected topicQuestionCount(topic: TopicNode): number {
    return topic.questionCount || topic.questions?.length || 0;
  }

  protected topicTotalQuestionCount(topic: TopicNode): number {
    let total = topic.questionCount || topic.questions?.length || 0;
    for (const child of topic.children || []) {
      total += this.topicTotalQuestionCount(child);
    }
    return total;
  }

  protected async selectChildTopic(topic: TopicNode): Promise<void> {
    if (!this.selectedSubject) {
      return;
    }

    this.selectedTopic = topic;
    this.topicQuestionLoadError = '';
    await this.ensureTopicQuestionsLoaded(topic);
  }

  protected getNextTopicForSelected(): TopicNode | null {
    if (!this.selectedSubject || !this.selectedTopic || this.isTopicGroup(this.selectedTopic)) {
      return null;
    }

    const orderedLeafTopics = this.flattenLeafTopics(this.selectedSubject.children || []);
    const currentIndex = orderedLeafTopics.findIndex((topic) => topic.id === this.selectedTopic?.id);
    if (currentIndex < 0 || currentIndex >= orderedLeafTopics.length - 1) {
      return null;
    }

    return orderedLeafTopics[currentIndex + 1] || null;
  }

  protected async goToNextTopic(): Promise<void> {
    const nextTopic = this.getNextTopicForSelected();
    if (!nextTopic) {
      return;
    }

    this.selectedTopic = nextTopic;
    this.topicQuestionLoadError = '';
    await this.ensureTopicQuestionsLoaded(nextTopic);
    this.scrollToTopicTop();
  }

  protected async onTopicQuizCompleted(event: TopicQuizCompletionEvent): Promise<void> {
    const topicId = event.topicId;
    this.topicProgressSaveError = '';
    const saved = await this.learningDataService.markTopicCompleted(topicId, event.scorePercent);
    if (!saved) {
      this.topicProgressSaveError = 'Progress could not be saved. Please click Finish Quiz again.';
    } else {
      if (!this.completedTopicIds.includes(topicId)) {
        this.completedTopicIds = [...this.completedTopicIds, topicId];
      }
      this.progressRefreshService.notifyProgressUpdated();
    }

    this.cdr.detectChanges();
  }

  protected openTopicPicker(): void {
    this.isTopicPickerOpen = true;
    document.body.style.overflow = 'hidden';
  }

  protected closeTopicPicker(): void {
    this.isTopicPickerOpen = false;
    document.body.style.overflow = '';
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
  }

  private async getCompletedTopicIds(): Promise<string[]> {
    const completedTopicIds = await this.learningDataService.getCompletedTopicCodes();
    return Array.isArray(completedTopicIds) ? completedTopicIds : [];
  }

  private async ensureTopicQuestionsLoaded(topic: TopicNode): Promise<void> {
    if (topic.children?.length || topic.questions?.length) {
      return;
    }

    this.loadingTopicQuestions = true;
    this.topicQuestionLoadError = '';
    this.cdr.detectChanges();

    try {
      const questions = await this.learningDataService.getTopicQuestions(topic.id);
      if (!questions) {
        this.topicQuestionLoadError = 'Could not load questions for this topic.';
        return;
      }

      topic.questions = questions;
      topic.questionCount = questions.length;
      this.prefetchTopicImages(questions);
    } catch (err) {
      if (err instanceof Error && err.message === 'TOPIC_PREMIUM_FORBIDDEN') {
        this.topicQuestionLoadError = 'This topic requires premium subscription.';
        return;
      }

      this.topicQuestionLoadError = 'Could not load questions for this topic.';
    } finally {
      this.loadingTopicQuestions = false;
      this.cdr.detectChanges();
    }
  }

  private prefetchTopicImages(questions: { answerImageUrl?: string }[]): void {
    const imageUrls = questions
      .map((question) => question.answerImageUrl || '')
      .filter((url) => !!url);

    for (const imageUrl of imageUrls) {
      const image = new Image();
      image.loading = 'eager';
      image.src = imageUrl;
    }
  }

  private flattenLeafTopics(topics: TopicNode[]): TopicNode[] {
    const flattened: TopicNode[] = [];

    for (const topic of topics) {
      if (topic.children?.length) {
        flattened.push(...this.flattenLeafTopics(topic.children));
      } else {
        flattened.push(topic);
      }
    }

    return flattened;
  }

  private getInitialTopic(subject: TopicNode): TopicNode | null {
    const leafTopics = this.flattenLeafTopics(subject.children || []);
    if (leafTopics.length > 0) {
      return leafTopics[0];
    }

    return subject.questions?.length ? subject : null;
  }

  private scrollToTopicTop(): void {
    requestAnimationFrame(() => {
      const topicOverview = document.querySelector('.topic-overview');
      if (topicOverview instanceof HTMLElement) {
        topicOverview.scrollIntoView({ behavior: 'smooth', block: 'start' });
        return;
      }

      window.scrollTo({ top: 0, behavior: 'smooth' });
    });
  }
}
