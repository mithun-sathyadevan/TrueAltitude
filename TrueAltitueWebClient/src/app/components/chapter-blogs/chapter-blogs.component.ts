import { Component, Input } from '@angular/core';

import { TopicNode } from '../../models/learning.models';

@Component({
  selector: 'app-chapter-blogs',
  standalone: true,
  templateUrl: './chapter-blogs.component.html',
  styleUrl: './chapter-blogs.component.scss',
})
export class ChapterBlogsComponent {
  @Input() topic: TopicNode | null = null;
}
