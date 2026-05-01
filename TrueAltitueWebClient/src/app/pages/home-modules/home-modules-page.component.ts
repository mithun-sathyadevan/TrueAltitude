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

@Component({
  selector: 'app-home-modules-page',
  standalone: true,
  imports: [CommonModule, RouterLink, TopHeaderComponent],
  templateUrl: './home-modules-page.component.html',
  styleUrl: './home-modules-page.component.scss',
})
export class HomeModulesPageComponent {
  protected readonly moduleCards: ModuleCard[] = [
    {
      title: 'Topic Wise Learning',
      summary: 'Navigate topics and subtopics, answer MCQs, and review explanations.',
      iconClass: 'fas fa-sitemap',
      route: '/learning/subjects',
      available: true,
    },
    {
      title: 'Real-Time Exam Quiz',
      summary: 'Practice in timed mode with pass/fail evaluation and instant review.',
      iconClass: 'fas fa-stopwatch',
      route: '/learning/realtime-exam',
      available: true,
    },
    {
      title: 'Upcoming Module',
      summary: 'Reserved for future learning modules you add later.',
      iconClass: 'fas fa-plus-circle',
      available: false,
    },
  ];
}
