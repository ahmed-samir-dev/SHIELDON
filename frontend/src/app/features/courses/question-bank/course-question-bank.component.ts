import { Component, Input, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { QuestionBankService } from '../services/question-bank.service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { AddOptionRequest, AddQuestionRequest, ExamQuestion, UpdateQuestionRequest } from '../../../core/models/question.model';

@Component({
  selector: 'app-course-question-bank',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './course-question-bank.component.html',
  styleUrl: './course-question-bank.component.scss'
})
export class CourseQuestionBankComponent implements OnInit {
  @Input({ required: true }) courseId!: string;

  private questionBankService = inject(QuestionBankService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);

  questions = signal<ExamQuestion[]>([]);
  mcqCount = computed(() => this.questions().filter(q => q.type === 'MCQ').length);
  tfCount = computed(() => this.questions().filter(q => q.type === 'TrueFalse').length);
  saCount = computed(() => this.questions().filter(q => q.type === 'ShortAnswer').length);

  isLoading = signal(true);
  isSubmitting = signal(false);
  
  isModalOpen = signal(false);
  editingQuestionId = signal<string | null>(null);

  questionForm: FormGroup;

  // Pagination
  currentPage = signal(1);
  pageSize = signal(12);

  paginatedQuestions = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize();
    return this.questions().slice(start, start + this.pageSize());
  });

  totalPages = computed(() => Math.ceil(this.questions().length / this.pageSize()) || 1);

  // View Details Modal
  viewingQuestion = signal<ExamQuestion | null>(null);

  constructor() {
    this.questionForm = this.fb.group({
      type: ['MCQ', Validators.required],
      questionText: ['', Validators.required],
      points: [1, [Validators.required, Validators.min(1)]],
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
    this.questionBankService.getQuestions(this.courseId).subscribe({
      next: (res) => {
        this.questions.set(res.data || []);
        this.currentPage.set(1);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load questions');
        this.isLoading.set(false);
      }
    });
  }

  openCreateModal() {
    this.editingQuestionId.set(null);
    this.questionForm.reset({
      type: 'MCQ',
      points: 1,
      isRandomized: true,
      trueFalseCorrectAnswer: true
    });
    this.questionForm.get('type')?.enable();
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
    this.editingQuestionId.set(question.id);
    
    // Clear options
    this.optionsFormArray.clear();

    // Pre-fill options based on the question type
    if (question.type === 'MCQ') {
      question.options.forEach(opt => {
        this.optionsFormArray.push(this.fb.group({
          optionText: [opt.optionText, Validators.required],
          isCorrect: [opt.isCorrect]
        }));
      });
    }

    const isTrueCorrect = question.type === 'TrueFalse' 
      ? question.options.find(o => o.optionText === 'True')?.isCorrect || false
      : true;

    this.questionForm.patchValue({
      type: question.type,
      questionText: question.questionText,
      points: question.points,
      isRandomized: question.isRandomized,
      trueFalseCorrectAnswer: isTrueCorrect
    });
    this.questionForm.get('type')?.disable();

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
    } else {
      // Clear options so hidden empty fields don't invalidate the form
      this.optionsFormArray.clear();
    }
  }

  onSubmit() {
    if (this.questionForm.invalid) {
      this.questionForm.markAllAsTouched();
      return;
    }

    const formValue = this.questionForm.getRawValue();

    this.isSubmitting.set(true);

    if (this.editingQuestionId()) {
      // Update existing
      const updateReq: UpdateQuestionRequest = {
        questionText: formValue.questionText,
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
        updateReq.options = options;
      } else if (formValue.type === 'TrueFalse') {
        updateReq.trueFalseCorrectAnswer = formValue.trueFalseCorrectAnswer === 'true' || formValue.trueFalseCorrectAnswer === true;
      }

      this.questionBankService.updateQuestion(this.courseId, this.editingQuestionId()!, updateReq).subscribe({
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

      this.questionBankService.addQuestion(this.courseId, req).subscribe({
        next: () => {
          this.toastr.success('Question added successfully');
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
        this.questionBankService.deleteQuestion(this.courseId, questionId).subscribe({
          next: () => {
            this.toastr.success('Question deleted successfully');
            this.loadQuestions();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to delete question');
          }
        });
      }
    });
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  openViewDetailsModal(question: ExamQuestion) {
    this.viewingQuestion.set(question);
  }

  closeViewDetailsModal() {
    this.viewingQuestion.set(null);
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
