export interface QuestionOption {
  id: string;
  optionText: string;
  isCorrect: boolean;
}

export interface ExamQuestion {
  id: string;
  examId: string;
  questionText: string;
  type: string; // "MCQ" | "TrueFalse" | "ShortAnswer"
  points: number;
  orderIndex: number;
  isRandomized: boolean;
  options: QuestionOption[];
}

export interface AddOptionRequest {
  optionText: string;
  isCorrect: boolean;
}

export interface AddQuestionRequest {
  questionText: string;
  type: string;
  points: number;
  isRandomized: boolean;
  options?: AddOptionRequest[];
  trueFalseCorrectAnswer?: boolean;
}

export interface UpdateQuestionRequest {
  questionText?: string;
  points?: number;
  isRandomized?: boolean;
}

export interface ReorderQuestionsRequest {
  items: { questionId: string; orderIndex: number }[];
}

export interface UpdateOptionRequest {
  optionText?: string;
  isCorrect?: boolean;
}
