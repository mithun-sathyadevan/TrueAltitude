import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import { Question, CreateQuestionRequest, UpdateQuestionRequest, Subject, Topic, LinkedTopic } from '../../models/admin.models';

@Component({
  selector: 'app-admin-questions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-questions.component.html',
  styleUrls: ['./admin-questions.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminQuestionsComponent implements OnInit {
  questions: Question[] = [];
  subjects: Subject[] = [];
  topicsBySubjectId: Record<number, Topic[]> = {};
  linkingSubjectIdByQuestionId: Record<number, number | null> = {};
  linkingTopicIdByQuestionId: Record<number, number | null> = {};
  linkingSortOrderByQuestionId: Record<number, number> = {};
  loading = false;
  showForm = false;
  formMode: 'create' | 'edit' = 'create';
  currentPage = 1;
  pageSize = 10;
  totalQuestions = 0;
  searchQuery = '';
  appliedSearchQuery = '';
  
  formData: CreateQuestionRequest = {
    questionText: '',
    type: 'multiple_choice',
    explanationText: '',
    difficulty: 1,
    options: [
      { optionText: '', isCorrect: false },
      { optionText: '', isCorrect: false }
    ]
  };
  
  editingId: number | null = null;
  questionTypes = ['multiple_choice', 'true_false', 'short_answer'];
  difficultyLevels = [1, 2, 3, 4, 5];

  constructor(
    private adminService: AdminService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSubjects();
    this.loadQuestions();
  }

  loadSubjects(): void {
    this.adminService.getAllSubjects().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.subjects = response.data;
          this.initializeLinkState();
        }
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading subjects:', error);
        this.cdr.markForCheck();
      }
    });
  }

  loadQuestions(): void {
    this.loading = true;
    this.cdr.markForCheck();
    this.adminService.getAllQuestions(this.currentPage, this.pageSize, this.appliedSearchQuery).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.questions = response.data.data.map(question => ({
            ...question,
            linkedTopics: question.linkedTopics ?? []
          }));
          this.totalQuestions = response.data.total || 0;
          this.initializeLinkState();
        }
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading questions:', error);
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

  openEditForm(question: Question): void {
    this.formMode = 'edit';
    this.editingId = question.id;
    this.formData = {
      questionText: question.questionText,
      type: question.type,
      explanationText: question.explanationText,
      difficulty: question.difficulty,
      options: question.options.map(o => ({ optionText: o.optionText, isCorrect: o.isCorrect }))
    };
    this.showForm = true;
    this.cdr.markForCheck();
  }

  resetForm(): void {
    this.formData = {
      questionText: '',
      type: 'multiple_choice',
      explanationText: '',
      difficulty: 1,
      options: [
        { optionText: '', isCorrect: false },
        { optionText: '', isCorrect: false }
      ]
    };
  }

  addOption(): void {
    this.formData.options.push({ optionText: '', isCorrect: false });
    this.cdr.markForCheck();
  }

  removeOption(index: number): void {
    if (this.formData.options.length > 2) {
      this.formData.options.splice(index, 1);
      this.cdr.markForCheck();
    }
  }

  saveQuestion(): void {
    // Validate at least one correct answer
    const hasCorrectAnswer = this.formData.options.some(o => o.isCorrect);
    if (!hasCorrectAnswer) {
      alert('Please mark at least one option as correct');
      return;
    }

    if (this.formMode === 'create') {
      this.adminService.createQuestion(this.formData).subscribe({
        next: () => {
          alert('Question created successfully');
          this.showForm = false;
          this.loadQuestions();
        },
        error: (error) => {
          console.error('Error creating question:', error);
          alert('Failed to create question');
        }
      });
    } else if (this.editingId) {
      const updateRequest: UpdateQuestionRequest = { ...this.formData, id: this.editingId };
      this.adminService.updateQuestion(updateRequest).subscribe({
        next: () => {
          alert('Question updated successfully');
          this.showForm = false;
          this.loadQuestions();
        },
        error: (error) => {
          console.error('Error updating question:', error);
          alert('Failed to update question');
        }
      });
    }
  }

  deleteQuestion(id: number): void {
    if (confirm('Are you sure you want to delete this question?')) {
      this.adminService.deleteQuestion(id).subscribe({
        next: () => {
          alert('Question deleted successfully');
          this.loadQuestions();
        },
        error: (error) => {
          console.error('Error deleting question:', error);
          alert('Failed to delete question');
        }
      });
    }
  }

  closeForm(): void {
    this.showForm = false;
    this.resetForm();
    this.cdr.markForCheck();
  }

  applyServerFilters(): void {
    this.currentPage = 1;
    this.appliedSearchQuery = this.searchQuery;
    this.loadQuestions();
  }

  onLinkSubjectChange(questionId: number, subjectId: number | null): void {
    this.linkingSubjectIdByQuestionId[questionId] = subjectId;
    this.linkingTopicIdByQuestionId[questionId] = null;

    if (subjectId) {
      this.loadTopicsForSubject(subjectId);
    }

    this.cdr.markForCheck();
  }

  loadTopicsForSubject(subjectId: number): void {
    this.adminService.getTopicsBySubjectId(subjectId).subscribe({
      next: (response) => {
        this.topicsBySubjectId[subjectId] = response.success && response.data ? response.data : [];
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading topics for linking:', error);
        this.topicsBySubjectId[subjectId] = [];
        this.cdr.markForCheck();
      }
    });
  }

  getAvailableTopics(question: Question): Topic[] {
    const subjectId = this.linkingSubjectIdByQuestionId[question.id];
    if (!subjectId) {
      return [];
    }

    const subjectTopics = this.topicsBySubjectId[subjectId] ?? [];
    const parentTopicIds = new Set(
      subjectTopics
        .map(topic => topic.parentTopicId)
        .filter((parentTopicId): parentTopicId is number => parentTopicId !== null && parentTopicId !== undefined)
    );
    const linkedTopicIds = new Set(question.linkedTopics.map(topic => topic.id));

    return subjectTopics.filter(topic => !linkedTopicIds.has(topic.id) && !parentTopicIds.has(topic.id));
  }

  linkQuestionToSelectedTopic(question: Question): void {
    const topicId = this.linkingTopicIdByQuestionId[question.id];
    const sortOrder = this.linkingSortOrderByQuestionId[question.id] ?? 0;

    if (!topicId) {
      alert('Please choose a topic to link.');
      return;
    }

    this.adminService.linkQuestionToTopic(topicId, question.id, sortOrder).subscribe({
      next: () => {
        alert('Question linked to topic successfully');
        this.loadQuestions();
      },
      error: (error) => {
        console.error('Error linking question to topic:', error);
        alert('Failed to link question to topic');
      }
    });
  }

  unlinkQuestionFromTopic(question: Question, linkedTopic: LinkedTopic): void {
    this.adminService.unlinkQuestionFromTopic(linkedTopic.id, question.id).subscribe({
      next: () => {
        alert('Question unlinked from topic successfully');
        this.loadQuestions();
      },
      error: (error) => {
        console.error('Error unlinking question from topic:', error);
        alert('Failed to unlink question from topic');
      }
    });
  }

  private initializeLinkState(): void {
    if (this.questions.length === 0) {
      return;
    }

    for (const question of this.questions) {
      const firstLinkedTopic = question.linkedTopics[0];

      if (firstLinkedTopic) {
        this.linkingSubjectIdByQuestionId[question.id] = firstLinkedTopic.subjectId;
        this.linkingSortOrderByQuestionId[question.id] = firstLinkedTopic.sortOrder + 1;
        this.loadTopicsForSubject(firstLinkedTopic.subjectId);
      } else if (
        this.subjects.length > 0 &&
        (this.linkingSubjectIdByQuestionId[question.id] === undefined || this.linkingSubjectIdByQuestionId[question.id] === null)
      ) {
        const defaultSubjectId = this.subjects[0].id;
        this.linkingSubjectIdByQuestionId[question.id] = defaultSubjectId;
        this.linkingSortOrderByQuestionId[question.id] = 0;
        this.loadTopicsForSubject(defaultSubjectId);
      }

      if (this.linkingSortOrderByQuestionId[question.id] === undefined) {
        this.linkingSortOrderByQuestionId[question.id] = 0;
      }
    }
  }

  getDifficultyLabel(difficulty: number): string {
    const labels: { [key: number]: string } = {
      1: 'Easy',
      2: 'Moderate',
      3: 'Medium',
      4: 'Hard',
      5: 'Very Hard'
    };
    return labels[difficulty] || 'Unknown';
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadQuestions();
    }
  }

  nextPage(): void {
    const maxPage = Math.ceil(this.totalQuestions / this.pageSize);
    if (this.currentPage < maxPage) {
      this.currentPage++;
      this.loadQuestions();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalQuestions / this.pageSize));
  }
}
