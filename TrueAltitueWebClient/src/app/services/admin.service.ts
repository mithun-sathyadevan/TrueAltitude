import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AdminUser,
  Subject,
  Topic,
  Question,
  CreateSubjectRequest,
  UpdateSubjectRequest,
  CreateTopicRequest,
  UpdateTopicRequest,
  CreateQuestionRequest,
  UpdateQuestionRequest,
  LinkQuestionRequest,
  PaginatedResponse,
  ApiResponse,
  UpdateUserRoleRequest,
  ToggleUserStatusRequest
} from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private apiUrl = 'api/admin';

  constructor(private http: HttpClient) {}

  // ===== User Management =====

  getAllUsers(page: number = 1, pageSize: number = 20, searchQuery: string = '', role: string = ''): Observable<ApiResponse<PaginatedResponse<AdminUser>>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (searchQuery.trim()) {
      params = params.set('searchQuery', searchQuery.trim());
    }

    if (role.trim()) {
      params = params.set('role', role.trim());
    }

    return this.http.get<ApiResponse<PaginatedResponse<AdminUser>>>(`${this.apiUrl}/users`, { params });
  }

  getUserById(userId: number): Observable<ApiResponse<AdminUser>> {
    return this.http.get<ApiResponse<AdminUser>>(`${this.apiUrl}/users/${userId}`);
  }

  updateUserRole(userId: number, role: string): Observable<ApiResponse<void>> {
    const request: UpdateUserRoleRequest = { userId, role };
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/users/${userId}/role`, request);
  }

  toggleUserStatus(userId: number, isActive: boolean): Observable<ApiResponse<void>> {
    const request: ToggleUserStatusRequest = { userId, isActive };
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/users/${userId}/status`, request);
  }

  // ===== Subject Management =====

  createSubject(request: CreateSubjectRequest): Observable<ApiResponse<Subject>> {
    return this.http.post<ApiResponse<Subject>>(`${this.apiUrl}/subjects`, request);
  }

  getAllSubjects(): Observable<ApiResponse<Subject[]>> {
    return this.http.get<ApiResponse<Subject[]>>(`${this.apiUrl}/subjects`);
  }

  getSubjectById(subjectId: number): Observable<ApiResponse<Subject>> {
    return this.http.get<ApiResponse<Subject>>(`${this.apiUrl}/subjects/${subjectId}`);
  }

  updateSubject(request: UpdateSubjectRequest): Observable<ApiResponse<Subject>> {
    return this.http.put<ApiResponse<Subject>>(`${this.apiUrl}/subjects/${request.id}`, request);
  }

  deleteSubject(subjectId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/subjects/${subjectId}`);
  }

  // ===== Topic Management =====

  createTopic(request: CreateTopicRequest): Observable<ApiResponse<Topic>> {
    return this.http.post<ApiResponse<Topic>>(`${this.apiUrl}/topics`, request);
  }

  getTopicsBySubjectId(subjectId: number): Observable<ApiResponse<Topic[]>> {
    return this.http.get<ApiResponse<Topic[]>>(`${this.apiUrl}/subjects/${subjectId}/topics`);
  }

  getTopicById(topicId: number): Observable<ApiResponse<Topic>> {
    return this.http.get<ApiResponse<Topic>>(`${this.apiUrl}/topics/${topicId}`);
  }

  updateTopic(request: UpdateTopicRequest): Observable<ApiResponse<Topic>> {
    return this.http.put<ApiResponse<Topic>>(`${this.apiUrl}/topics/${request.id}`, request);
  }

  deleteTopic(topicId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/topics/${topicId}`);
  }

  // ===== Question Management =====

  createQuestion(request: CreateQuestionRequest): Observable<ApiResponse<Question>> {
    return this.http.post<ApiResponse<Question>>(`${this.apiUrl}/questions`, request);
  }

  getAllQuestions(page: number = 1, pageSize: number = 20, searchQuery: string = ''): Observable<ApiResponse<PaginatedResponse<Question>>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (searchQuery.trim()) {
      params = params.set('searchQuery', searchQuery.trim());
    }

    return this.http.get<ApiResponse<PaginatedResponse<Question>>>(`${this.apiUrl}/questions`, { params });
  }

  getQuestionById(questionId: number): Observable<ApiResponse<Question>> {
    return this.http.get<ApiResponse<Question>>(`${this.apiUrl}/questions/${questionId}`);
  }

  updateQuestion(request: UpdateQuestionRequest): Observable<ApiResponse<Question>> {
    return this.http.put<ApiResponse<Question>>(`${this.apiUrl}/questions/${request.id}`, request);
  }

  deleteQuestion(questionId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/questions/${questionId}`);
  }

  // ===== Link Question to Topic =====

  linkQuestionToTopic(topicId: number, questionId: number, sortOrder: number): Observable<ApiResponse<void>> {
    const request = { topicId, questionId, sortOrder };
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/topics/${topicId}/questions/${questionId}`, request);
  }

  unlinkQuestionFromTopic(topicId: number, questionId: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/topics/${topicId}/questions/${questionId}`);
  }
}
