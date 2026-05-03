import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import { Subject, CreateSubjectRequest, UpdateSubjectRequest } from '../../models/admin.models';

@Component({
  selector: 'app-admin-subjects',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-subjects.component.html',
  styleUrls: ['./admin-subjects.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminSubjectsComponent implements OnInit {
  subjects: Subject[] = [];
  filteredSubjects: Subject[] = [];
  loading = false;
  showForm = false;
  formMode: 'create' | 'edit' = 'create';
  searchQuery = '';
  
  formData: CreateSubjectRequest = {
    code: '',
    title: '',
    description: '',
    requiresSubscription: false,
    subscriptionLabel: '',
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
    this.loading = true;
    this.cdr.markForCheck();
    this.adminService.getAllSubjects().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.subjects = response.data;
          this.applyFilters();
        }
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading subjects:', error);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openCreateForm(): void {
    this.formMode = 'create';
    this.editingId = null;
    this.resetForm();
    this.showForm = true;
    this.cdr.markForCheck();
  }

  applyFilters(): void {
    const normalizedSearch = this.searchQuery.trim().toLowerCase();

    if (!normalizedSearch) {
      this.filteredSubjects = [...this.subjects];
      this.cdr.markForCheck();
      return;
    }

    this.filteredSubjects = this.subjects.filter(subject =>
      subject.title.toLowerCase().includes(normalizedSearch) ||
      subject.code.toLowerCase().includes(normalizedSearch) ||
      subject.description.toLowerCase().includes(normalizedSearch)
    );
    this.cdr.markForCheck();
  }

  openEditForm(subject: Subject): void {
    this.formMode = 'edit';
    this.editingId = subject.id;
    this.formData = { ...subject };
    this.showForm = true;
    this.cdr.markForCheck();
  }

  resetForm(): void {
    this.formData = {
      code: '',
      title: '',
      description: '',
      requiresSubscription: false,
      subscriptionLabel: '',
      sortOrder: 0
    };
  }

  saveSubject(): void {
    if (this.formMode === 'create') {
      this.adminService.createSubject(this.formData).subscribe({
        next: () => {
          alert('Subject created successfully');
          this.showForm = false;
          this.cdr.markForCheck();
          this.loadSubjects();
        },
        error: (error) => {
          console.error('Error creating subject:', error);
          alert('Failed to create subject');
        }
      });
    } else if (this.editingId) {
      const updateRequest: UpdateSubjectRequest = { ...this.formData, id: this.editingId };
      this.adminService.updateSubject(updateRequest).subscribe({
        next: () => {
          alert('Subject updated successfully');
          this.showForm = false;
          this.cdr.markForCheck();
          this.loadSubjects();
        },
        error: (error) => {
          console.error('Error updating subject:', error);
          alert('Failed to update subject');
        }
      });
    }
  }

  deleteSubject(id: number): void {
    if (confirm('Are you sure you want to delete this subject?')) {
      this.adminService.deleteSubject(id).subscribe({
        next: () => {
          alert('Subject deleted successfully');
          this.cdr.markForCheck();
          this.loadSubjects();
        },
        error: (error) => {
          console.error('Error deleting subject:', error);
          alert('Failed to delete subject');
        }
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.resetForm();
    this.cdr.markForCheck();
  }
}
