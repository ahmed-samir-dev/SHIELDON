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
export class QuestionBankService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getQuestions(courseId: string): Observable<ApiResponse<ExamQuestion[]>> {
    return this.http.get<ApiResponse<ExamQuestion[]>>(`${this.apiUrl}/courses/${courseId}/question-bank`);
  }

  getBankCounts(courseId: string): Observable<ApiResponse<{ [key: string]: number }>> {
    return this.http.get<ApiResponse<{ [key: string]: number }>>(`${this.apiUrl}/courses/${courseId}/question-bank/counts`);
  }

  addQuestion(courseId: string, request: AddQuestionRequest): Observable<ApiResponse<ExamQuestion>> {
    return this.http.post<ApiResponse<ExamQuestion>>(`${this.apiUrl}/courses/${courseId}/question-bank`, request);
  }

  updateQuestion(courseId: string, questionId: string, request: UpdateQuestionRequest): Observable<ApiResponse<ExamQuestion>> {
    return this.http.patch<ApiResponse<ExamQuestion>>(`${this.apiUrl}/courses/${courseId}/question-bank/${questionId}`, request);
  }

  deleteQuestion(courseId: string, questionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/courses/${courseId}/question-bank/${questionId}`);
  }

  reorderQuestions(courseId: string, request: ReorderQuestionsRequest): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/courses/${courseId}/question-bank/reorder`, request);
  }

  addOption(courseId: string, questionId: string, request: AddOptionRequest): Observable<ApiResponse<QuestionOption>> {
    return this.http.post<ApiResponse<QuestionOption>>(`${this.apiUrl}/courses/${courseId}/question-bank/${questionId}/options`, request);
  }

  updateOption(courseId: string, questionId: string, optionId: string, request: UpdateOptionRequest): Observable<ApiResponse<QuestionOption>> {
    return this.http.patch<ApiResponse<QuestionOption>>(`${this.apiUrl}/courses/${courseId}/question-bank/${questionId}/options/${optionId}`, request);
  }

  deleteOption(courseId: string, questionId: string, optionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/courses/${courseId}/question-bank/${questionId}/options/${optionId}`);
  }
}
