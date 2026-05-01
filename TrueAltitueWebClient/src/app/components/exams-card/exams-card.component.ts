import { Component } from '@angular/core';

interface ExamEvent {
  date: string;
  title: string;
}

@Component({
  selector: 'app-exams-card',
  standalone: true,
  templateUrl: './exams-card.component.html',
  styleUrl: './exams-card.component.scss',
})
export class ExamsCardComponent {
  protected readonly exams: ExamEvent[] = [
    { date: 'May 15', title: 'Navigation Systems' },
    { date: 'May 22', title: 'Aircraft Maintenance' },
  ];
}
