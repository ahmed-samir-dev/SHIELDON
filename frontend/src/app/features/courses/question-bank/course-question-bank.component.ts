import { Component, Input, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { QuestionBankService } from '../services/question-bank.service';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { AddOptionRequest, AddQuestionRequest, ExamQuestion, UpdateQuestionRequest } from '../../../core/models/question.model';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-course-question-bank',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './course-question-bank.component.html',
  styleUrl: './course-question-bank.component.scss'
})
export class CourseQuestionBankComponent implements OnInit {
  @Input({ required: true }) courseId!: string;

  private questionBankService = inject(QuestionBankService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);

  questions = signal<ExamQuestion[]>([]);
  mcqCount = computed(() => this.questions().filter(q => q.type === 'MCQ').length);
  tfCount = computed(() => this.questions().filter(q => q.type === 'TrueFalse').length);
  saCount = computed(() => this.questions().filter(q => q.type === 'ShortAnswer').length);

  isLoading = signal(true);
  isSubmitting = signal(false);
  
  isModalOpen = signal(false);
  editingQuestionId = signal<string | null>(null);

  // Image Upload State
  selectedImage = signal<File | null>(null);
  imagePreview = signal<string | null>(null);
  deleteExistingImage = signal(false);

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
        this.toastr.error(err.error?.message || this.translate.instant('COURSE_QUESTION_BANK.TOAST_LOAD_ERR'));
        this.isLoading.set(false);
      }
    });
  }

  openCreateModal() {
    this.editingQuestionId.set(null);
    this.selectedImage.set(null);
    this.imagePreview.set(null);
    this.deleteExistingImage.set(false);
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
    this.selectedImage.set(null);
    this.imagePreview.set(question.imageUrl || null);
    this.deleteExistingImage.set(false);
    
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
    this.selectedImage.set(null);
    this.imagePreview.set(null);
    this.deleteExistingImage.set(false);
  }

  // --- Image Upload Handling ---

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFileSelection(files[0]);
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFileSelection(input.files[0]);
    }
  }

  private handleFileSelection(file: File) {
    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.toastr.error(this.translate.instant('Only JPG, PNG, GIF, and WEBP images are allowed.'));
      return;
    }
    // Validate file size (5MB max)
    if (file.size > 5 * 1024 * 1024) {
      this.toastr.error(this.translate.instant('Image must be less than 5MB.'));
      return;
    }

    this.selectedImage.set(file);
    this.deleteExistingImage.set(false);

    // Create preview
    const reader = new FileReader();
    reader.onload = (e) => {
      this.imagePreview.set(e.target?.result as string);
    };
    reader.readAsDataURL(file);
  }

  removeImage() {
    this.selectedImage.set(null);
    this.imagePreview.set(null);
    this.deleteExistingImage.set(true); // Flag to delete on backend if editing
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
      this.toastr.warning(this.translate.instant('COURSE_QUESTION_BANK.TOAST_MCQ_MIN'));
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
          this.toastr.error(this.translate.instant('COURSE_QUESTION_BANK.TOAST_MCQ_EXACTLY_ONE'));
          this.isSubmitting.set(false);
          return;
        }
        updateReq.options = options;
      } else if (formValue.type === 'TrueFalse') {
        updateReq.trueFalseCorrectAnswer = formValue.trueFalseCorrectAnswer === 'true' || formValue.trueFalseCorrectAnswer === true;
      }

      this.questionBankService.updateQuestion(this.courseId, this.editingQuestionId()!, updateReq).subscribe({
        next: () => {
          this.handleImageUploadAfterSave(this.editingQuestionId()!);
        },
        error: (err) => {
          this.toastr.error(err.error?.message || this.translate.instant('COURSE_QUESTION_BANK.TOAST_UPDATE_ERR'));
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
          this.toastr.error(this.translate.instant('COURSE_QUESTION_BANK.TOAST_MCQ_EXACTLY_ONE'));
          this.isSubmitting.set(false);
          return;
        }
        req.options = options;
      } else if (formValue.type === 'TrueFalse') {
        req.trueFalseCorrectAnswer = formValue.trueFalseCorrectAnswer === 'true' || formValue.trueFalseCorrectAnswer === true;
      }

      this.questionBankService.addQuestion(this.courseId, req).subscribe({
        next: (res) => {
          this.handleImageUploadAfterSave(res.data!.id);
        },
        error: (err) => {
          this.toastr.error(err.error?.message || this.translate.instant('COURSE_QUESTION_BANK.TOAST_ADD_ERR'));
          this.isSubmitting.set(false);
        }
      });
    }
  }

  private handleImageUploadAfterSave(questionId: string) {
    if (this.selectedImage()) {
      // Upload new image
      this.questionBankService.uploadImage(this.courseId, questionId, this.selectedImage()!).subscribe({
        next: () => {
          this.toastr.success(this.editingQuestionId() ? this.translate.instant('COURSE_QUESTION_BANK.TOAST_UPDATE_SUCCESS') : this.translate.instant('COURSE_QUESTION_BANK.TOAST_ADD_SUCCESS'));
          this.finishSubmit();
        },
        error: (err) => {
          this.toastr.error('Question saved, but image upload failed: ' + (err.error?.message || 'Unknown error'));
          this.finishSubmit();
        }
      });
    } else if (this.deleteExistingImage() && this.editingQuestionId()) {
      // Delete existing image
      this.questionBankService.deleteImage(this.courseId, questionId).subscribe({
        next: () => {
          this.toastr.success(this.translate.instant('COURSE_QUESTION_BANK.TOAST_UPDATE_SUCCESS'));
          this.finishSubmit();
        },
        error: (err) => {
          this.toastr.error('Question updated, but failed to delete image.');
          this.finishSubmit();
        }
      });
    } else {
      // No image changes
      this.toastr.success(this.editingQuestionId() ? this.translate.instant('COURSE_QUESTION_BANK.TOAST_UPDATE_SUCCESS') : this.translate.instant('COURSE_QUESTION_BANK.TOAST_ADD_SUCCESS'));
      this.finishSubmit();
    }
  }

  private finishSubmit() {
    this.closeModal();
    this.loadQuestions();
    this.isSubmitting.set(false);
  }

  deleteQuestion(questionId: string) {
    Swal.fire({
      title: this.translate.instant('COURSE_QUESTION_BANK.SWAL_DEL_TITLE'),
      text: this.translate.instant('COURSE_QUESTION_BANK.SWAL_DEL_TEXT'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: this.translate.instant('COURSE_QUESTION_BANK.SWAL_BTN_DEL')
    }).then((result) => {
      if (result.isConfirmed) {
        this.questionBankService.deleteQuestion(this.courseId, questionId).subscribe({
          next: () => {
            this.toastr.success(this.translate.instant('COURSE_QUESTION_BANK.TOAST_DEL_SUCCESS'));
            this.loadQuestions();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('COURSE_QUESTION_BANK.TOAST_DEL_ERR'));
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
      case 'MCQ': return this.translate.instant('COURSE_QUESTION_BANK.TYPE_MCQ');
      case 'TrueFalse': return this.translate.instant('COURSE_QUESTION_BANK.TYPE_TF');
      case 'ShortAnswer': return this.translate.instant('COURSE_QUESTION_BANK.TYPE_SA');
      default: return type;
    }
  }
}
