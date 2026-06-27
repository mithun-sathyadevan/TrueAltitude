import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { TopicProgressSummary } from '../../models/learning.models';
import { TopHeaderComponent } from '../../components/top-header/top-header.component';
import { LearningDataService } from '../../services/learning-data.service';
import { ProgressRefreshService } from '../../services/progress-refresh.service';

interface ModuleCard {
  key: 'learning' | 'timedExam' | 'performanceInsights';
  title: string;
  summary: string;
  iconClass: string;
  route?: string;
  available: boolean;
}

@Component({
  selector: 'app-home-modules-page',
  standalone: true,
  imports: [CommonModule, RouterLink, TopHeaderComponent],
  templateUrl: './home-modules-page.component.html',
  styleUrl: './home-modules-page.component.scss',
})
export class HomeModulesPageComponent implements OnInit, OnDestroy {
  protected readonly flightGreeting = this.getFlightGreeting();
  protected readonly timedExamModeEnabled = true;

  protected topicProgress: TopicProgressSummary = {
    totalTopics: 0,
    coveredTopics: 0,
    remainingTopics: 0,
    coveragePercent: 0,
  };

  protected readonly moduleCards: ModuleCard[] = [
    {
      key: 'learning',
      title: 'Learning Track',
      summary: 'Study by subject and topic, answer MCQs, and review explanations instantly.',
      iconClass: 'fas fa-sitemap',
      route: '/learning/subjects',
      available: true,
    },
    {
      key: 'timedExam',
      title: 'Timed Exam Mode',
      summary: 'Attempt a focused 60-minute quiz session and evaluate your readiness.',
      iconClass: 'fas fa-stopwatch',
      route: '/learning/realtime-exam',
      available: this.timedExamModeEnabled,
    },
    {
      key: 'performanceInsights',
      title: 'Performance Insights',
      summary: 'Open detailed analytics with weak-area guidance and focused recommendations.',
      iconClass: 'fas fa-plus-circle',
      route: '/learning/performance-insights',
      available: true,
    },
  ];

  private readonly subscriptions = new Subscription();

  constructor(
    private readonly learningDataService: LearningDataService,
    private readonly progressRefreshService: ProgressRefreshService,
    private readonly router: Router,
    private readonly ref: ChangeDetectorRef,
  ) {}

  async ngOnInit(): Promise<void> {
    await this.loadDashboardSignals();
    this.ref.detectChanges();

    this.subscriptions.add(
      this.router.events.subscribe((event) => {
        if (event instanceof NavigationEnd && (event.urlAfterRedirects === '/' || event.urlAfterRedirects.startsWith('/?'))) {
          void this.loadDashboardSignals().then(() => {
            this.ref.detectChanges();
          });
        }
      }),
    );

    this.subscriptions.add(
      this.progressRefreshService.progressUpdated$.subscribe(() => {
        void this.loadDashboardSignals().then(() => {
          this.ref.detectChanges();
        });
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private async loadDashboardSignals(): Promise<void> {
    await this.loadTopicProgress();
  }

  private async loadTopicProgress(): Promise<void> {
    const progress = await this.learningDataService.getTopicProgressSummary();
    if (!progress) {
      return;
    }

    this.topicProgress = progress;
    this.ref.detectChanges();
  }

  private getFlightGreeting(): string {
    const hour = new Date().getHours();

    if (hour < 12) {
      return 'Good morning, Captain';
    }

    if (hour < 18) {
      return 'Good afternoon, Captain';
    }

    return 'Good evening, Captain';
  }
}
