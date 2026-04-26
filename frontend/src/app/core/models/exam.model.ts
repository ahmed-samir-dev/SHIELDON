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
  scheduledReleaseAt?: string;
  questionCount: number;
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
  scheduledReleaseAt?: string;
  questionCount: number;
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
  scheduledReleaseAt?: string | null;
}

export interface UpdateExamRequest {
  title?: string;
  instructions?: string;
  timeLimit?: number;
  maxAttempts?: number;
  passScore?: number;
  resultVisibility?: 'Immediate' | 'Scheduled' | 'ManualRelease';
  scheduledAt?: string | null;
  scheduledReleaseAt?: string | null;
}
