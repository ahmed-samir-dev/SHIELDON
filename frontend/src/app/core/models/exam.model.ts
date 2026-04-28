export interface ExamSelectionRule {
  id?: string;
  questionType: string;
  count: number;
}

export interface ExamSummaryResponse {
  id: string;
  courseId: string;
  courseTitle: string;
  title: string;
  instructions?: string;
  timeLimit: number;
  maxAttempts: number;
  passScore: number;
  status: 'Draft' | 'Published' | 'Closed';
  resultVisibility: 'Immediate' | 'Scheduled' | 'ManualRelease';
  scheduledAt?: string;
  scheduledEndAt?: string;
  scheduledReleaseAt?: string;
  bankQuestionCount: number;
  selectionRules: ExamSelectionRule[];
  createdAt: string;
}

export interface ExamDetailResponse {
  id: string;
  courseId: string;
  courseTitle: string;
  title: string;
  instructions?: string;
  timeLimit: number;
  maxAttempts: number;
  passScore: number;
  status: 'Draft' | 'Published' | 'Closed';
  resultVisibility: 'Immediate' | 'Scheduled' | 'ManualRelease';
  scheduledAt?: string;
  scheduledEndAt?: string;
  scheduledReleaseAt?: string;
  bankQuestionCount: number;
  selectionRules: ExamSelectionRule[];
  createdByName: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateExamRequest {
  title: string;
  instructions?: string;
  timeLimit: number;
  maxAttempts: number;
  passScore: number;
  resultVisibility: 'Immediate' | 'Scheduled' | 'ManualRelease';
  scheduledAt?: string | null;
  scheduledEndAt?: string | null;
  scheduledReleaseAt?: string | null;
  selectionRules?: ExamSelectionRule[];
}

export interface UpdateExamRequest {
  title?: string;
  instructions?: string;
  timeLimit?: number;
  maxAttempts?: number;
  passScore?: number;
  resultVisibility?: 'Immediate' | 'Scheduled' | 'ManualRelease';
  scheduledAt?: string | null;
  scheduledEndAt?: string | null;
  scheduledReleaseAt?: string | null;
  selectionRules?: ExamSelectionRule[];
}
