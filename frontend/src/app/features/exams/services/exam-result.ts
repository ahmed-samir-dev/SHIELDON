import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ExamResultResponse {
  attemptId: string;
  examId: string;
  courseId: string;
  examTitle: string;
  courseTitle: string;
  status: 'InProgress' | 'Submitted' | 'Graded' | 'ForceSubmitted';
  startedAt: string;
  submittedAt: string | null;
  score: number | null;
  passScore: number;
  passed: boolean | null;
  isPublished: boolean;
  resultVisible: boolean;
  questionReviews: QuestionReviewDto[] | null;
  canRequestReattempt: boolean;
}

export interface QuestionReviewDto {
  questionId: string;
  questionText: string;
  imageUrl?: string | null;
  type: 'MCQ' | 'TrueFalse' | 'ShortAnswer';
  points: number;
  pointsAwarded: number | null;
  isCorrect: boolean | null;
  selectedOptionId: string | null;
  selectedOptionText: string | null;
  correctOptionId: string | null;
  correctOptionText: string | null;
  textAnswer: string | null;
  requiresManualGrading: boolean;
}

export interface ExamAttemptSummaryDto {
  attemptId: string;
  studentId: string;
  studentName: string;
  studentDisplayId: string;
  status: 'InProgress' | 'Submitted' | 'Graded' | 'ForceSubmitted';
  startedAt: string;
  submittedAt: string | null;
  score: number | null;
  passed: boolean | null;
  isGradePublished: boolean;
  attemptNumber: number;
  notes?: string | null;
}

export interface GradeShortAnswerRequest {
  grades: {
    questionId: string;
    pointsAwarded: number;
  }[];
}

export interface ReleaseResultsRequest {
  studentIds?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ExamResultService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}`;

  getAttemptResult(attemptId: string): Observable<{ data: ExamResultResponse }> {
    return this.http.get<{ data: ExamResultResponse }>(`${this.apiUrl}/exam-attempts/${attemptId}/result`);
  }

  getExamAttempts(examId: string): Observable<{ data: ExamAttemptSummaryDto[] }> {
    return this.http.get<{ data: ExamAttemptSummaryDto[] }>(`${this.apiUrl}/exams/${examId}/attempts`);
  }

  getStudentAttempts(examId: string): Observable<{ data: ExamAttemptSummaryDto[] }> {
    return this.http.get<{ data: ExamAttemptSummaryDto[] }>(`${this.apiUrl}/exams/${examId}/my-attempts`);
  }

  gradeShortAnswers(attemptId: string, request: GradeShortAnswerRequest): Observable<{ data: string, message: string }> {
    return this.http.post<{ data: string, message: string }>(`${this.apiUrl}/exam-attempts/${attemptId}/grade-short-answers`, request);
  }

  releaseResults(examId: string, request: ReleaseResultsRequest = {}): Observable<{ data: string, message: string }> {
    return this.http.post<{ data: string, message: string }>(`${this.apiUrl}/exams/${examId}/release-results`, request);
  }

  exportResultsCsv(examId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/exams/${examId}/export`, { responseType: 'blob' });
  }
}
