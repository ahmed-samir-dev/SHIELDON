import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  AddOptionRequest,
  AddQuestionRequest,
  ExamQuestion,
  QuestionOption,
  ReorderQuestionsRequest,
  UpdateOptionRequest,
  UpdateQuestionRequest
} from '../../../core/models/question.model';

@Injectable({
  providedIn: 'root'
})
export class QuestionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api`;

  getQuestions(examId: string): Observable<ApiResponse<ExamQuestion[]>> {
    return this.http.get<ApiResponse<ExamQuestion[]>>(`${this.apiUrl}/exams/${examId}/questions`);
  }

  addQuestion(examId: string, request: AddQuestionRequest): Observable<ApiResponse<ExamQuestion>> {
    return this.http.post<ApiResponse<ExamQuestion>>(`${this.apiUrl}/exams/${examId}/questions`, request);
  }

  updateQuestion(examId: string, questionId: string, request: UpdateQuestionRequest): Observable<ApiResponse<ExamQuestion>> {
    return this.http.patch<ApiResponse<ExamQuestion>>(`${this.apiUrl}/exams/${examId}/questions/${questionId}`, request);
  }

  deleteQuestion(examId: string, questionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/exams/${examId}/questions/${questionId}`);
  }

  reorderQuestions(examId: string, request: ReorderQuestionsRequest): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/exams/${examId}/questions/reorder`, request);
  }

  addOption(questionId: string, request: AddOptionRequest): Observable<ApiResponse<QuestionOption>> {
    return this.http.post<ApiResponse<QuestionOption>>(`${this.apiUrl}/questions/${questionId}/options`, request);
  }

  updateOption(questionId: string, optionId: string, request: UpdateOptionRequest): Observable<ApiResponse<QuestionOption>> {
    return this.http.patch<ApiResponse<QuestionOption>>(`${this.apiUrl}/questions/${questionId}/options/${optionId}`, request);
  }

  deleteOption(questionId: string, optionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/questions/${questionId}/options/${optionId}`);
  }
}
