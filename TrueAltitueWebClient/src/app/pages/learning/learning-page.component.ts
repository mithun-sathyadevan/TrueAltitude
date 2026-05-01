import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ChapterBlogsComponent } from '../../components/chapter-blogs/chapter-blogs.component';
import { TopicQuizComponent } from '../../components/topic-quiz/topic-quiz.component';
import { TopicTreeComponent } from '../../components/topic-tree/topic-tree.component';
import { VideoCourseComponent } from '../../components/video-course/video-course.component';
import { TopicNode } from '../../models/learning.models';
import { LearningDataService } from '../../services/learning-data.service';

@Component({
  selector: 'app-learning-page',
  standalone: true,
  imports: [CommonModule, RouterLink, TopicTreeComponent, TopicQuizComponent, VideoCourseComponent, ChapterBlogsComponent],
  templateUrl: './learning-page.component.html',
  styleUrl: './learning-page.component.scss',
})
export class LearningPageComponent {
  protected selectedSubject!: TopicNode;
  protected selectedTopic!: TopicNode;
  protected isTopicPickerOpen = false;

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly router: Router,
    private readonly learningDataService: LearningDataService,
  ) {
    this.activatedRoute.paramMap.subscribe((params) => {
      const subjectId = params.get('subjectId');
      if (!subjectId) {
        void this.router.navigate(['/learning/subjects']);
        return;
      }

      const subject = this.learningDataService.getSubjectById(subjectId);
      if (!subject || subject.requiresSubscription) {
        void this.router.navigate(['/learning/subjects']);
        return;
      }

      this.selectedSubject = subject;
      this.selectedTopic = this.learningDataService.findInitialTopic(subject.children || [], subject);
    });
  }

  protected onTopicSelected(topic: TopicNode): void {
    this.selectedTopic = topic;
    this.closeTopicPicker();
  }

  protected isTopicGroup(topic: TopicNode): boolean {
    return !!topic.children?.length && !topic.questions?.length;
  }

  protected hasQuestions(topic: TopicNode): boolean {
    return !!topic.questions?.length;
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
    if (topic.requiresSubscription) {
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
