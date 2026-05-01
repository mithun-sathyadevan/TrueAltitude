import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { TopicNode } from '../../models/learning.models';
import { LearningDataService } from '../../services/learning-data.service';

@Component({
  selector: 'app-learning-subjects-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './learning-subjects-page.component.html',
  styleUrl: './learning-subjects-page.component.scss',
})
export class LearningSubjectsPageComponent {
  protected subjectQuery = '';
  protected readonly subjects: TopicNode[];

  constructor(private readonly learningDataService: LearningDataService) {
    this.subjects = this.learningDataService.getSubjects();
  }

  protected get filteredSubjects(): TopicNode[] {
    const query = this.subjectQuery.trim().toLowerCase();
    if (!query) {
      return this.subjects;
    }

    return this.subjects.filter(
      (subject) =>
        subject.title.toLowerCase().includes(query) || subject.description.toLowerCase().includes(query),
    );
  }
}
