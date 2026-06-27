import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { TopicNode } from '../../models/learning.models';
import { AuthService } from '../../services/auth.service';
import { LearningDataService } from '../../services/learning-data.service';
import { SubscriptionAccessService } from '../../services/subscription-access.service';

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
}
