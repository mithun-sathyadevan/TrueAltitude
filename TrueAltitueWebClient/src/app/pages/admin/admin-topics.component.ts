import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import { Subject, Topic, CreateTopicRequest, UpdateTopicRequest } from '../../models/admin.models';

@Component({
  selector: 'app-admin-topics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-topics.component.html',
  styleUrls: ['./admin-topics.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminTopicsComponent implements OnInit {
  subjects: Subject[] = [];
  topics: Topic[] = [];
  filteredTopics: Topic[] = [];
  availableParentTopics: Topic[] = [];
  loading = false;
  showForm = false;
  formMode: 'create' | 'edit' = 'create';
  selectedSubjectId: number | null = null;
  
  formData: CreateTopicRequest = {
    subjectId: 0,
    parentTopicId: null,
    code: '',
    title: '',
    description: '',
    sortOrder: 0
  };
  
  editingId: number | null = null;

  constructor(
    private adminService: AdminService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSubjects();
  }

  loadSubjects(): void {
    this.adminService.getAllSubjects().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.subjects = response.data;

          if (!this.selectedSubjectId && this.subjects.length > 0) {
            this.selectedSubjectId = this.subjects[0].id;
            this.formData.subjectId = this.selectedSubjectId;
            this.loadTopics();
          }

          this.cdr.markForCheck();
        }
      },
      error: (error) => {
        console.error('Error loading subjects:', error);
        this.cdr.markForCheck();
      }
    });
  }

  loadTopics(): void {
    if (!this.selectedSubjectId) return;
    
    this.loading = true;
    this.cdr.markForCheck();
    this.adminService.getTopicsBySubjectId(this.selectedSubjectId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.topics = response.data;
          this.filteredTopics = response.data;

          if (this.formData.subjectId === this.selectedSubjectId) {
            this.refreshAvailableParentTopics(this.topics);
          }
        }
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading topics:', error);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onSubjectChange(): void {
    if (this.selectedSubjectId) {
      this.formData.subjectId = this.selectedSubjectId;
      this.formData.parentTopicId = null;
      this.cdr.markForCheck();
      this.loadTopics();
      return;
    }

    this.topics = [];
    this.filteredTopics = [];
    this.availableParentTopics = [];
    this.cdr.markForCheck();
  }

  openCreateForm(): void {
    if (!this.selectedSubjectId) {
      alert('Please select a subject first');
      return;
    }
    this.formMode = 'create';
    this.editingId = null;
    this.resetForm();
    this.refreshAvailableParentTopics(this.topics);
    this.showForm = true;
    this.cdr.markForCheck();
  }

  openEditForm(topic: Topic): void {
    this.formMode = 'edit';
    this.editingId = topic.id;
    this.formData = {
      subjectId: topic.subjectId,
      parentTopicId: topic.parentTopicId ?? null,
      code: topic.code,
      title: topic.title,
      description: topic.description,
      sortOrder: topic.sortOrder
    };

    if (topic.subjectId === this.selectedSubjectId) {
      this.refreshAvailableParentTopics(this.topics);
    } else {
      this.loadAvailableParentTopics(topic.subjectId);
    }

    this.showForm = true;
    this.cdr.markForCheck();
  }

  resetForm(): void {
    this.formData = {
      subjectId: this.selectedSubjectId || 0,
      parentTopicId: null,
      code: '',
      title: '',
      description: '',
      sortOrder: 0
    };
  }

  onFormSubjectChange(): void {
    this.formData.parentTopicId = null;
    if (this.formData.subjectId) {
      this.loadAvailableParentTopics(this.formData.subjectId);
    } else {
      this.availableParentTopics = [];
      this.cdr.markForCheck();
    }
  }

  private loadAvailableParentTopics(subjectId: number): void {
    if (!subjectId) {
      this.availableParentTopics = [];
      this.cdr.markForCheck();
      return;
    }

    if (subjectId === this.selectedSubjectId) {
      this.refreshAvailableParentTopics(this.topics);
      this.cdr.markForCheck();
      return;
    }

    this.adminService.getTopicsBySubjectId(subjectId).subscribe({
      next: (response) => {
        this.refreshAvailableParentTopics(response.success && response.data ? response.data : []);
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading parent topics:', error);
        this.availableParentTopics = [];
        this.cdr.markForCheck();
      }
    });
  }

  private refreshAvailableParentTopics(topics: Topic[]): void {
    this.availableParentTopics = topics.filter(topic => topic.id !== this.editingId);

    if (
      this.formData.parentTopicId &&
      !this.availableParentTopics.some(topic => topic.id === this.formData.parentTopicId)
    ) {
      this.formData.parentTopicId = null;
    }
  }

  saveTopic(): void {
    if (this.formMode === 'create') {
      this.adminService.createTopic(this.formData).subscribe({
        next: () => {
          alert('Topic created successfully');
          this.selectedSubjectId = this.formData.subjectId;
          this.showForm = false;
          this.loadTopics();
        },
        error: (error) => {
          console.error('Error creating topic:', error);
          alert('Failed to create topic');
        }
      });
    } else if (this.editingId) {
      const updateRequest: UpdateTopicRequest = { ...this.formData, id: this.editingId };
      this.adminService.updateTopic(updateRequest).subscribe({
        next: () => {
          alert('Topic updated successfully');
          this.selectedSubjectId = this.formData.subjectId;
          this.showForm = false;
          this.loadTopics();
        },
        error: (error) => {
          console.error('Error updating topic:', error);
          alert('Failed to update topic');
        }
      });
    }
  }

  deleteTopic(id: number): void {
    if (confirm('Are you sure you want to delete this topic?')) {
      this.adminService.deleteTopic(id).subscribe({
        next: () => {
          alert('Topic deleted successfully');
          this.loadTopics();
        },
        error: (error) => {
          console.error('Error deleting topic:', error);
          alert('Failed to delete topic');
        }
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.resetForm();
    this.cdr.markForCheck();
  }

  getChildCount(topicId: number): number {
    return this.topics.filter(topic => topic.parentTopicId === topicId).length;
  }
}
