import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { TopHeaderComponent } from '../../components/top-header/top-header.component';

interface ModuleCard {
  title: string;
  summary: string;
  iconClass: string;
  route?: string;
  available: boolean;
}

interface TopicProgressStat {
  totalTopics: number;
  coveredTopics: number;
  remainingTopics: number;
  coveragePercent: number;
}

@Component({
  selector: 'app-home-modules-page',
  standalone: true,
  imports: [CommonModule, RouterLink, TopHeaderComponent],
  templateUrl: './home-modules-page.component.html',
  styleUrl: './home-modules-page.component.scss',
})
export class HomeModulesPageComponent {
  protected readonly flightGreeting = this.getFlightGreeting();

  protected readonly topicProgress: TopicProgressStat = {
    totalTopics: 24,
    coveredTopics: 16,
    remainingTopics: 8,
    coveragePercent: 67,
  };

  protected readonly moduleCards: ModuleCard[] = [
    {
      title: 'Learning Track',
      summary: 'Study by subject and topic, answer MCQs, and review explanations instantly.',
      iconClass: 'fas fa-sitemap',
      route: '/learning/subjects',
      available: true,
    },
    {
      title: 'Timed Exam Mode',
      summary: 'Attempt a focused 60-minute quiz session and evaluate your readiness.',
      iconClass: 'fas fa-stopwatch',
      route: '/learning/realtime-exam',
      available: true,
    },
    {
      title: 'Performance Insights',
      summary: 'Detailed analytics and weak-area recommendations will be available here.',
      iconClass: 'fas fa-plus-circle',
      available: false,
    },
  ];

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
