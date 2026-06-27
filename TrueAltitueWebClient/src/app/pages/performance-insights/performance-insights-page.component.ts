import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { TopicPerformanceInsight } from '../../models/learning.models';
import { LearningDataService } from '../../services/learning-data.service';
import { ProgressRefreshService } from '../../services/progress-refresh.service';

@Component({
  selector: 'app-performance-insights-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './performance-insights-page.component.html',
  styleUrl: './performance-insights-page.component.scss',
})
export class PerformanceInsightsPageComponent implements OnInit, OnDestroy {
  protected isLoading = true;
  protected loadError = '';
  protected insight: TopicPerformanceInsight = {
    attemptedTopics: 0,
    averageScorePercent: 0,
    consistencyPercent: 0,
    recommendation: 'Finish at least one topic quiz to unlock score-based insights.',
    strongTopics: [],
    improvementTopics: [],
  };

  private readonly subscriptions = new Subscription();

  constructor(
    private readonly learningDataService: LearningDataService,
    private readonly progressRefreshService: ProgressRefreshService,
    private readonly ref: ChangeDetectorRef,
  ) {}

  async ngOnInit(): Promise<void> {
    await this.loadInsight();
    this.ref.detectChanges();

    this.subscriptions.add(
      this.progressRefreshService.progressUpdated$.subscribe(() => {
        void this.loadInsight().then(() => {
          this.ref.detectChanges();
        });
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  protected get hasInsightData(): boolean {
    return this.insight.attemptedTopics > 0;
  }

  private async loadInsight(): Promise<void> {
    this.isLoading = true;
    this.loadError = '';
    this.ref.detectChanges();

    try {
      const insight = await this.learningDataService.getTopicPerformanceInsight();
      if (!insight) {
        this.loadError = 'Unable to load performance insights right now. Please try again.';
        return;
      }

      this.insight = insight;
    } finally {
      this.isLoading = false;
      this.ref.detectChanges();
    }
  }
}
