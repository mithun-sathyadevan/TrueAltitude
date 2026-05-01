import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

import { TopicNode } from '../../models/learning.models';

@Component({
  selector: 'app-topic-tree',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './topic-tree.component.html',
  styleUrl: './topic-tree.component.scss',
})
export class TopicTreeComponent {
  @Input({ required: true }) topics: TopicNode[] = [];
  @Input() depth = 0;
  @Input() selectedTopicId: string | null = null;
  @Output() topicSelected = new EventEmitter<TopicNode>();

  protected onTopicSelected(topic: TopicNode): void {
    if (topic.requiresSubscription) {
      return;
    }

    this.topicSelected.emit(topic);
  }
}
