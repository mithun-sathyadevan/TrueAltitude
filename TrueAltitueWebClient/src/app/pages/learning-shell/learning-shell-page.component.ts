import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { TopHeaderComponent } from '../../components/top-header/top-header.component';

interface LearningModuleNavItem {
  label: string;
  description: string;
  route: string;
  available: boolean;
}

@Component({
  selector: 'app-learning-shell-page',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, TopHeaderComponent],
  templateUrl: './learning-shell-page.component.html',
  styleUrl: './learning-shell-page.component.scss',
})
export class LearningShellPageComponent {
  protected isModulePopupOpen = false;
  protected isSubjectsRoute = false;
  protected readonly timedExamModeEnabled = true;

  protected readonly modules: LearningModuleNavItem[] = [
    {
      label: 'Topic Wise Learning',
      description: 'Explore hierarchical topics, subtopics, MCQ, videos, and chapter blogs.',
      route: '/learning/subjects',
      available: true,
    },
    {
      label: 'Real-Time Exam Quiz',
      description: 'Timed exam simulation with pass/fail insights and explanations.',
      route: '/learning/realtime-exam',
      available: this.timedExamModeEnabled,
    },
    {
      label: 'Performance Insights',
      description: 'Score trends, strong topics, and improvement guidance based on your quizzes.',
      route: '/learning/performance-insights',
      available: true,
    },
    {
      label: 'Future Module Slot',
      description: 'Reserved for upcoming learning modules and assessments.',
      route: '/learning/future-module',
      available: false,
    },
  ];

  constructor(private readonly router: Router) {
    this.updateRouteState(this.router.url);

    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.updateRouteState(event.urlAfterRedirects);
      }
    });
  }

  protected toggleModulePopup(): void {
    this.isModulePopupOpen = !this.isModulePopupOpen;
  }

  protected closeModulePopup(): void {
    this.isModulePopupOpen = false;
  }

  private updateRouteState(url: string): void {
    this.isSubjectsRoute = url.startsWith('/learning/subjects');
    if (this.isSubjectsRoute) {
      this.closeModulePopup();
    }
  }
}
