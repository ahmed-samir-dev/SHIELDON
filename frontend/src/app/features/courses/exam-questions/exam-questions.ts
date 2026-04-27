import { Component, EventEmitter, Input, OnInit, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { QuestionService } from '../services/question.service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { ExamSummaryResponse } from '../../../core/models/exam.model';
import { AddOptionRequest, AddQuestionRequest, ExamQuestion, ReorderQuestionsRequest, UpdateQuestionRequest } from '../../../core/models/question.model';

@Component({
  selector: 'app-exam-questions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './exam-questions.html',
  styleUrl: './exam-questions.scss'
})
export class ExamQuestionsComponent implements OnInit {
  @Input({ required: true }) exam!: ExamSummaryResponse;
  @Output() close = new EventEmitter<void>();

  private questionService = inject(QuestionService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);

  questions = signal<ExamQuestion[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);
  
  isModalOpen = signal(false);
  editingQuestionId = signal<string | null>(null);

  questionForm: FormGroup;

  // Track drag and drop state for reordering
  draggedIndex: number | null = null;

  constructor() {
    this.questionForm = this.fb.group({
      type: ['MCQ', Validators.required],
      questionText: ['', Validators.required],
      points: [1, [Validators.required, Validators.min(0.1)]],
      isRandomized: [true],
      // For MCQ
      options: this.fb.array([]),
      // For TrueFalse
      trueFalseCorrectAnswer: [true]
    });
  }

  ngOnInit() {
    this.loadQuestions();
  }

  get optionsFormArray() {
    return this.questionForm.get('options') as FormArray;
  }

  loadQuestions() {
    this.isLoading.set(true);
    this.questionService.getQuestions(this.exam.id).subscribe({
      next: (res) => {
        this.questions.set(res.data || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load questions');
        this.isLoading.set(false);
      }
    });
  }

  openCreateModal() {
    if (this.exam.status !== 'Draft') {
      this.toastr.warning('Questions can only be added to Draft exams.');
      return;
    }
    this.editingQuestionId.set(null);
    this.questionForm.reset({
      type: 'MCQ',
      points: 1,
      isRandomized: true,
      trueFalseCorrectAnswer: true
    });
    this.optionsFormArray.clear();
    // Default 4 options for MCQ
    for (let i = 0; i < 4; i++) {
      this.addOptionControl();
    }
    // Set first option as correct by default
    if (this.optionsFormArray.length > 0) {
      this.optionsFormArray.at(0).patchValue({ isCorrect: true });
    }
    this.isModalOpen.set(true);
  }

  openEditModal(question: ExamQuestion) {
    if (this.exam.status !== 'Draft') {
      this.toastr.warning('Questions can only be edited on Draft exams.');
      return;
    }
    this.editingQuestionId.set(question.id);
    
    // Clear options
    this.optionsFormArray.clear();

    // We can only edit questionText, points, isRandomized for existing questions via PATCH
    // Editing options for MCQ is done via separate endpoints, but for simplicity in UI, 
    // we might just disable type changing.
    this.questionForm.patchValue({
      type: question.type,
      questionText: question.questionText,
      points: question.points,
      isRandomized: question.isRandomized
    });

    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
    this.editingQuestionId.set(null);
  }

  addOptionControl() {
    this.optionsFormArray.push(this.fb.group({
      optionText: ['', Validators.required],
      isCorrect: [false]
    }));
  }

  removeOptionControl(index: number) {
    if (this.optionsFormArray.length > 2) {
      this.optionsFormArray.removeAt(index);
    } else {
      this.toastr.warning('MCQ questions must have at least 2 options.');
    }
  }

  setCorrectOption(index: number) {
    for (let i = 0; i < this.optionsFormArray.length; i++) {
      this.optionsFormArray.at(i).patchValue({ isCorrect: i === index });
    }
  }

  onTypeChange(event: Event) {
    const type = (event.target as HTMLSelectElement).value;
    if (type === 'MCQ') {
      if (this.optionsFormArray.length === 0) {
        for (let i = 0; i < 4; i++) this.addOptionControl();
        this.optionsFormArray.at(0).patchValue({ isCorrect: true });
      }
    }
  }

  onSubmit() {
    if (this.questionForm.invalid) {
      this.questionForm.markAllAsTouched();
      return;
    }

    const formValue = this.questionForm.value;

    this.isSubmitting.set(true);

    if (this.editingQuestionId()) {
      // Update existing
      const updateReq: UpdateQuestionRequest = {
        questionText: formValue.questionText,
        points: formValue.points,
        isRandomized: formValue.isRandomized
      };

      this.questionService.updateQuestion(this.exam.id, this.editingQuestionId()!, updateReq).subscribe({
        next: () => {
          this.toastr.success('Question updated successfully');
          this.finishSubmit();
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Failed to update question');
          this.isSubmitting.set(false);
        }
      });
    } else {
      // Create new
      const req: AddQuestionRequest = {
        questionText: formValue.questionText,
        type: formValue.type,
        points: formValue.points,
        isRandomized: formValue.isRandomized
      };

      if (formValue.type === 'MCQ') {
        const options: AddOptionRequest[] = formValue.options.map((o: any) => ({
          optionText: o.optionText,
          isCorrect: !!o.isCorrect
        }));
        
        const correctCount = options.filter(o => o.isCorrect).length;
        if (correctCount !== 1) {
          this.toastr.error('MCQ must have exactly 1 correct option.');
          this.isSubmitting.set(false);
          return;
        }
        req.options = options;
      } else if (formValue.type === 'TrueFalse') {
        req.trueFalseCorrectAnswer = formValue.trueFalseCorrectAnswer === 'true' || formValue.trueFalseCorrectAnswer === true;
      }

      this.questionService.addQuestion(this.exam.id, req).subscribe({
        next: () => {
          this.toastr.success('Question added successfully');
          // Increment the question count directly in the parent object so publish checking works
          this.exam.questionCount++;
          this.finishSubmit();
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Failed to add question');
          this.isSubmitting.set(false);
        }
      });
    }
  }

  private finishSubmit() {
    this.closeModal();
    this.loadQuestions();
    this.isSubmitting.set(false);
  }

  deleteQuestion(questionId: string) {
    if (this.exam.status !== 'Draft') {
      this.toastr.warning('Questions can only be deleted from Draft exams.');
      return;
    }

    Swal.fire({
      title: 'Delete Question?',
      text: 'Are you sure you want to delete this question? This cannot be undone.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, delete it'
    }).then((result) => {
      if (result.isConfirmed) {
        this.questionService.deleteQuestion(this.exam.id, questionId).subscribe({
          next: () => {
            this.toastr.success('Question deleted successfully');
            this.exam.questionCount--;
            this.loadQuestions();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to delete question');
          }
        });
      }
    });
  }

  // ── Drag and Drop Reordering ──────────────────────────────────────────────

  onDragStart(index: number) {
    if (this.exam.status !== 'Draft') return;
    this.draggedIndex = index;
  }

  onDragOver(event: DragEvent, index: number) {
    if (this.exam.status !== 'Draft') return;
    event.preventDefault(); // Necessary to allow dropping
  }

  onDrop(event: DragEvent, dropIndex: number) {
    if (this.exam.status !== 'Draft') return;
    event.preventDefault();
    if (this.draggedIndex !== null && this.draggedIndex !== dropIndex) {
      const qs = [...this.questions()];
      const movedItem = qs.splice(this.draggedIndex, 1)[0];
      qs.splice(dropIndex, 0, movedItem);
      
      // Update local array immediately for UI feedback
      this.questions.set(qs);
      
      // Call API
      const req: ReorderQuestionsRequest = {
        items: qs.map((q, idx) => ({ questionId: q.id, orderIndex: idx + 1 }))
      };

      this.questionService.reorderQuestions(this.exam.id, req).subscribe({
        next: () => {
          // Success silently
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Failed to reorder questions');
          this.loadQuestions(); // Reload to restore previous order
        }
      });
    }
    this.draggedIndex = null;
  }

  getBadgeClass(type: string): string {
    switch(type) {
      case 'MCQ': return 'badge-primary';
      case 'TrueFalse': return 'badge-teal';
      case 'ShortAnswer': return 'badge-warning';
      default: return 'badge-secondary';
    }
  }

  formatType(type: string): string {
    switch(type) {
      case 'MCQ': return 'Multiple Choice';
      case 'TrueFalse': return 'True/False';
      case 'ShortAnswer': return 'Short Answer';
      default: return type;
    }
  }
}
