import { Component, Input } from '@angular/core';

import { TopicNode } from '../../models/learning.models';

@Component({
  selector: 'app-video-course',
  standalone: true,
  templateUrl: './video-course.component.html',
  styleUrl: './video-course.component.scss',
})
export class VideoCourseComponent {
  @Input() topic: TopicNode | null = null;
}
