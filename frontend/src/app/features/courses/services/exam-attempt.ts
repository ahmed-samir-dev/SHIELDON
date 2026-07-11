import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Observable, tap } from 'rxjs';

export interface StudentOptionDto {
  id: string;
  text: string;
}

export enum QuestionType {
  MCQ = 'MCQ',
  TrueFalse = 'TrueFalse',
  ShortAnswer = 'ShortAnswer'
}

export interface StudentQuestionDto {
  id: string;
  text: string;
  imageUrl?: string;
  type: QuestionType;
  points: number;
  options: StudentOptionDto[];
}

export interface SavedAnswerDto {
  questionId: string;
  selectedOptionId?: string | null;
  textAnswer?: string | null;
}

export interface StartExamResponse {
  attemptId: string;
  token: string;
  timeLimitMinutes: number;
  passScore: number;
  expiresAt: string;
  questions: StudentQuestionDto[];
  savedAnswers: SavedAnswerDto[];
  courseId: string;
  resultVisibility: string;
  initialStrikeScore: number;
}

export interface SaveAnswerRequest {
  questionId: string;
  selectedOptionId?: string | null;
  textAnswer?: string | null;
}

export interface SubmitExamResponse {
  attemptId: string;
  status: string;
  score: number | null;
  passed: boolean;
  resultVisibility: string;
  courseId: string;
}

@Injectable({
  providedIn: 'root'
})
export class ExamAttemptService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // Store token in memory ONLY (strict security rule)
  private readonly _examToken = signal<string | null>(null);

  // Expose readonly signal for components to check if a token exists
  public readonly hasToken = signal<boolean>(false);

  startExam(examId: string): Observable<ApiResponse<StartExamResponse>> {
    return this.http.post<ApiResponse<StartExamResponse>>(`${this.apiUrl}/exams/${examId}/start`, {}).pipe(
      tap(response => {
        if (response.data && response.data.token) {
          this._examToken.set(response.data.token);
          this.hasToken.set(true);
        }
      })
    );
  }

  saveAnswer(attemptId: string, request: SaveAnswerRequest): Observable<ApiResponse<string>> {
    const headers = new HttpHeaders().set('X-Exam-Token', this._examToken() || '');
    return this.http.patch<ApiResponse<string>>(`${this.apiUrl}/exam-attempts/${attemptId}/answers`, request, { headers });
  }

  submitExam(attemptId: string): Observable<ApiResponse<SubmitExamResponse>> {
    const headers = new HttpHeaders().set('X-Exam-Token', this._examToken() || '');
    return this.http.post<ApiResponse<SubmitExamResponse>>(`${this.apiUrl}/exam-attempts/${attemptId}/submit`, {}, { headers }).pipe(
      tap(() => this.clearToken())
    );
  }

  forceSubmitExam(attemptId: string): Observable<ApiResponse<SubmitExamResponse>> {
    const headers = new HttpHeaders().set('X-Exam-Token', this._examToken() || '');
    return this.http.post<ApiResponse<SubmitExamResponse>>(`${this.apiUrl}/exam-attempts/${attemptId}/force-submit`, {}, { headers }).pipe(
      tap(() => this.clearToken())
    );
  }

  clearToken(): void {
    this._examToken.set(null);
    this.hasToken.set(false);
  }
}
