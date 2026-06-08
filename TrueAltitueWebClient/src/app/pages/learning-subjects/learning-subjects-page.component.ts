import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { TopicNode } from '../../models/learning.models';
import { AuthService } from '../../services/auth.service';
import { LearningDataService } from '../../services/learning-data.service';
import { SubscriptionAccessService } from '../../services/subscription-access.service';

type SubjectCompletionState = 'Completed' | 'In Progress' | 'Not Started';

interface SubjectCompletionItem {
  subject: string;
  completedTopics: number;
  totalTopics: number;
  state: SubjectCompletionState;
}

@Component({
  selector: 'app-learning-subjects-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './learning-subjects-page.component.html',
  styleUrl: './learning-subjects-page.component.scss',
})
export class LearningSubjectsPageComponent implements OnInit {
  protected subjectQuery = '';
  protected subjects: TopicNode[] = [];
  protected loading = true;
  protected loadError = '';

  protected readonly subjectCompletionItems: SubjectCompletionItem[] = [
    { subject: 'Air Navigation', completedTopics: 12, totalTopics: 12, state: 'Completed' },
    { subject: 'Meteorology', completedTopics: 9, totalTopics: 12, state: 'In Progress' },
    { subject: 'Flight Planning', completedTopics: 7, totalTopics: 10, state: 'In Progress' },
    { subject: 'Aircraft Systems', completedTopics: 10, totalTopics: 10, state: 'Completed' },
    { subject: 'Aviation Regulations', completedTopics: 4, totalTopics: 8, state: 'In Progress' },
    { subject: 'Radio Telephony', completedTopics: 0, totalTopics: 7, state: 'Not Started' },
    { subject: 'Performance and Limitations', completedTopics: 6, totalTopics: 11, state: 'In Progress' },
    { subject: 'Human Factors', completedTopics: 8, totalTopics: 8, state: 'Completed' },
    { subject: 'Air Law', completedTopics: 2, totalTopics: 9, state: 'In Progress' },
    { subject: 'Emergency Procedures', completedTopics: 0, totalTopics: 6, state: 'Not Started' },
  ];

  private readonly defaultSubjectCompletion: SubjectCompletionItem = {
    subject: '',
    completedTopics: 0,
    totalTopics: 0,
    state: 'Not Started',
  };

  constructor(
    private readonly learningDataService: LearningDataService,
    private readonly authService: AuthService,
    private readonly subscriptionAccessService: SubscriptionAccessService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  async ngOnInit(): Promise<void> {
    this.loading = true;
    this.loadError = '';

    try {
      const subjects = await this.learningDataService.getSubjects();
      if (!subjects) {
        this.loadError = 'Session expired. Please login again.';
        this.authService.logout();
        void this.router.navigate(['/login'], { queryParams: { returnUrl: '/learning/subjects' } });
        return;
      }

      if (!subjects.length) {
        this.loadError = 'No subjects found in database.';
      }

      this.subjects = subjects;
      this.loading = false;
      this.cdr.detectChanges();
    } catch (err) {
      console.error('[LearningSubjectsPageComponent] ngOnInit failed:', err);
      this.loadError = 'Session expired. Please login again.';
      this.authService.logout();
      void this.router.navigate(['/login'], { queryParams: { returnUrl: '/learning/subjects' } });
    } finally {
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  protected hasPremiumAccess(): boolean {
    return this.subscriptionAccessService.hasActiveSubscription();
  }

  protected onPremiumSubjectClick(subject: TopicNode): void {
    const targetUrl = `/learning/topics/${subject.id}`;

    if (this.hasPremiumAccess()) {
      void this.router.navigate(['/learning/topics', subject.id]);
      return;
    }

    this.subscriptionAccessService.redirectToSubscription(this.router, targetUrl);
  }

  protected trackSubject(index: number, subject: TopicNode): string {
    return subject.id || subject.title || `${index}`;
  }

  protected get filteredSubjects(): TopicNode[] {
    const query = this.subjectQuery.trim().toLowerCase();
    if (!query) {
      return this.subjects;
    }

    return this.subjects.filter(
      (subject) =>
        (subject.title || '').toLowerCase().includes(query) || (subject.description || '').toLowerCase().includes(query),
    );
  }

  protected getSubjectCompletion(subject: TopicNode): SubjectCompletionItem {
    const subjectTitle = (subject.title || '').trim().toLowerCase();
    const matched = this.subjectCompletionItems.find((item) => item.subject.trim().toLowerCase() === subjectTitle);
    return matched ?? this.defaultSubjectCompletion;
  }

  protected getSubjectCompletionPercent(subject: TopicNode): number {
    const item = this.getSubjectCompletion(subject);

    if (item.totalTopics <= 0) {
      return 0;
    }

    return Math.round((item.completedTopics / item.totalTopics) * 100);
  }
}
