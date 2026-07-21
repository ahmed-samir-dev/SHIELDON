import { Component, Input, OnInit, OnDestroy, inject, signal, ViewChild, ElementRef, Renderer2 } from '@angular/core';
import { CommonModule, DatePipe, DOCUMENT } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ExamService } from '../services/exam.service';
import { QuestionBankService } from '../services/question-bank.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import Swal from 'sweetalert2';
import { ToastrService } from 'ngx-toastr';
import { ExamSummaryResponse } from '../../../core/models/exam.model';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import { Router } from '@angular/router';
import { ExamResultService, ExamAttemptSummaryDto } from '../../exams/services/exam-result';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription, forkJoin, Observable } from 'rxjs';
import { ReattemptService, StudentReattemptStatusResponse } from '../../exams/services/reattempt.service';

export function atLeastOneQuestionValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const mcq = control.get('mcqCount')?.value || 0;
    const tf = control.get('trueFalseCount')?.value || 0;
    const sa = control.get('shortAnswerCount')?.value || 0;
    
    if (mcq + tf + sa <= 0) {
      return { noQuestionsSelected: true };
    }
    return null;
  };
}

@Component({
  selector: 'app-course-exams',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './course-exams.html',
  styleUrl: './course-exams.scss'
})
export class CourseExamsComponent implements OnInit, OnDestroy {
  @Input({ required: true }) course!: CourseDetailResponse;

  private examService = inject(ExamService);
  private questionBankService = inject(QuestionBankService);
  public authService = inject(AuthService);
  private languageService = inject(LanguageService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private translate = inject(TranslateService);
  private renderer = inject(Renderer2);
  private document = inject(DOCUMENT);
  private langSub!: Subscription;

  @ViewChild('examModalOverlay') examModalOverlayRef!: ElementRef<HTMLElement>;

  exams = signal<ExamSummaryResponse[]>([]);
  isLoading = signal(true);
  isSubmitting = signal(false);
  isModalOpen = signal(false);
  editingExamId = signal<string | null>(null);
  editingExam = signal<ExamSummaryResponse | null>(null);

  // Attempt Picker State
  showAttemptPicker = signal(false);
  studentAttemptsForPicker = signal<ExamAttemptSummaryDto[]>([]);
  pickerExamId = signal<string | null>(null);
  private examResultService = inject(ExamResultService);
  private reattemptService = inject(ReattemptService);

  // Student Requests State
  myRequests = signal<StudentReattemptStatusResponse[]>([]);

  currentTime = signal<Date>(new Date());
  private timeInterval: any;

  examForm: FormGroup;

  constructor() {
    this.examForm = this.fb.group({
      title: ['', Validators.required],
      instructions: [''],
      timeLimit: [60, [Validators.required, Validators.min(1)]],
      maxAttempts: [1, [Validators.required, Validators.min(1)]],
      passScore: [50, [Validators.required, Validators.min(0), Validators.max(100)]],
      weight: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      resultVisibility: ['Immediate', Validators.required],
      scheduledAt: [''],
      scheduledEndAt: [''],
      mcqCount: [0, [Validators.required, Validators.min(0)]],
      trueFalseCount: [0, [Validators.required, Validators.min(0)]],
      shortAnswerCount: [0, [Validators.required, Validators.min(0)]]
    }, { validators: atLeastOneQuestionValidator() });
  }

  ngOnInit() {
    this.loadExams();
    // 1-second tick: keeps all badges and buttons in real-time sync
    this.timeInterval = setInterval(() => {
      this.currentTime.set(new Date());
    }, 1000);
    // Auto-reload when user toggles language
    this.langSub = this.languageService.languageChange$.subscribe(() => this.loadExams());
  }

  ngOnDestroy() {
    if (this.timeInterval) clearInterval(this.timeInterval);
    this.langSub?.unsubscribe();
    const pickerEl = this.document.querySelector('.attempt-picker-overlay');
    if (pickerEl && pickerEl.parentElement === this.document.body) {
      this.renderer.removeChild(this.document.body, pickerEl);
    }
  }


  loadExams() {
    this.isLoading.set(true);
    
    const obs$: Observable<any>[] = [this.examService.getExams(this.course.id)];
    if (!this.canManageExams()) {
      obs$.push(this.reattemptService.getMyRequests());
    }

    forkJoin(obs$).subscribe({
      next: (results) => {
        const examsRes = results[0] as any;
        this.exams.set(examsRes.data?.items || []);
        
        if (results.length > 1) {
          const reqRes = results[1] as any;
          this.myRequests.set(reqRes.data || []);
        }
        
        this.isLoading.set(false);
      },
      error: () => {
        this.toastr.error(this.translate.instant('COURSE_EXAMS.TOAST_LOAD_ERR'));
        this.isLoading.set(false);
      }
    });
  }

  openCreateModal() {
    this.editingExamId.set(null);
    this.examForm.reset({
      timeLimit: 60,
      maxAttempts: 1,
      passScore: 50,
      weight: 0,
      resultVisibility: 'Immediate',
      mcqCount: 1,
      trueFalseCount: 0,
      shortAnswerCount: 0
    });
    this.isModalOpen.set(true);
    setTimeout(() => {
      const el = this.examModalOverlayRef?.nativeElement;
      if (el) {
        this.renderer.appendChild(this.document.body, el);
        requestAnimationFrame(() => {
          this.renderer.removeClass(el, 'modal-pending');
        });
      }
    }, 0);
  }

  openEditModal(exam: ExamSummaryResponse) {
    this.editingExamId.set(exam.id);
    this.editingExam.set(exam);

    // Convert UTC ISO strings to local datetime-local format (YYYY-MM-DDTHH:mm)
    // We MUST use local time here - <input type="datetime-local"> expects local, not UTC.
    const toLocalInput = (isoStr: string): string => {
      const d = new Date(isoStr);
      const pad = (n: number) => n.toString().padStart(2, '0');
      return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    };

    const mcqRule = exam.selectionRules?.find(r => r.questionType === 'MCQ');
    const tfRule = exam.selectionRules?.find(r => r.questionType === 'TrueFalse');
    const saRule = exam.selectionRules?.find(r => r.questionType === 'ShortAnswer');

    this.examForm.patchValue({
      title: exam.title,
      instructions: exam.instructions ?? '',
      timeLimit: exam.timeLimit,
      maxAttempts: exam.maxAttempts,
      passScore: exam.passScore,
      weight: exam.weight,
      resultVisibility: exam.resultVisibility,
      scheduledAt:    exam.scheduledAt    ? toLocalInput(exam.scheduledAt)    : '',
      scheduledEndAt: exam.scheduledEndAt ? toLocalInput(exam.scheduledEndAt) : '',
      mcqCount: mcqRule ? mcqRule.count : 0,
      trueFalseCount: tfRule ? tfRule.count : 0,
      shortAnswerCount: saRule ? saRule.count : 0
    });

    this.isModalOpen.set(true);
    setTimeout(() => {
      const el = this.examModalOverlayRef?.nativeElement;
      if (el) {
        this.renderer.appendChild(this.document.body, el);
        requestAnimationFrame(() => {
          this.renderer.removeClass(el, 'modal-pending');
        });
      }
    }, 0);
  }

  closeModal() {
    const el = this.examModalOverlayRef?.nativeElement;
    if (el) this.renderer.addClass(el, 'modal-pending');
    setTimeout(() => {
      if (el && el.parentElement === this.document.body) {
        this.renderer.removeChild(this.document.body, el);
      }
      this.isModalOpen.set(false);
      this.editingExamId.set(null);
      this.editingExam.set(null);
    }, 150);
  }


  onSubmit() {
    if (this.examForm.invalid) {
      this.examForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const formValue = this.examForm.value;

    this.questionBankService.getBankCounts(this.course.id).subscribe({
      next: (res) => {
        const counts = res.data || {};
        const availableMcq = counts['MCQ'] || 0;
        const availableTf = counts['TrueFalse'] || 0;
        const availableSa = counts['ShortAnswer'] || 0;

        if (formValue.mcqCount > availableMcq) {
          this.toastr.warning(this.translate.instant('COURSE_EXAMS.TOAST_MCQ_ERR')
            .replace('{selected}', formValue.mcqCount).replace('{available}', availableMcq));
          this.isSubmitting.set(false);
          return;
        }
        if (formValue.trueFalseCount > availableTf) {
          this.toastr.warning(this.translate.instant('COURSE_EXAMS.TOAST_TF_ERR')
            .replace('{selected}', formValue.trueFalseCount).replace('{available}', availableTf));
          this.isSubmitting.set(false);
          return;
        }
        if (formValue.shortAnswerCount > availableSa) {
          this.toastr.warning(this.translate.instant('COURSE_EXAMS.TOAST_SA_ERR')
            .replace('{selected}', formValue.shortAnswerCount).replace('{available}', availableSa));
          this.isSubmitting.set(false);
          return;
        }

        const selectionRules = [
          { questionType: 'MCQ', count: formValue.mcqCount },
          { questionType: 'TrueFalse', count: formValue.trueFalseCount },
          { questionType: 'ShortAnswer', count: formValue.shortAnswerCount }
        ].filter(r => r.count > 0);

        const request = {
          ...formValue,
          scheduledAt: formValue.scheduledAt ? new Date(formValue.scheduledAt).toISOString() : null,
          scheduledEndAt: formValue.scheduledEndAt ? new Date(formValue.scheduledEndAt).toISOString() : null,
          selectionRules
        };

        if (this.editingExamId()) {
          this.examService.updateExam(this.editingExamId()!, request).subscribe({
            next: () => {
              this.toastr.success(this.translate.instant('COURSE_EXAMS.TOAST_UPDATE_SUCCESS'));
              this.finishSubmit();
            },
            error: (err) => {
              this.toastr.error(err.error?.message || this.translate.instant('COURSE_EXAMS.TOAST_UPDATE_ERR'));
              this.isSubmitting.set(false);
            }
          });
        } else {
          this.examService.createExam(this.course.id, request).subscribe({
            next: () => {
              this.toastr.success(this.translate.instant('COURSE_EXAMS.TOAST_CREATE_SUCCESS'));
              this.finishSubmit();
            },
            error: (err) => {
              this.toastr.error(err.error?.message || this.translate.instant('COURSE_EXAMS.TOAST_CREATE_ERR'));
              this.isSubmitting.set(false);
            }
          });
        }
      },
      error: () => {
        this.toastr.error(this.translate.instant('COURSE_EXAMS.TOAST_VALIDATE_ERR'));
        this.isSubmitting.set(false);
      }
    });
  }

  private finishSubmit() {
    this.closeModal();
    this.loadExams();
    this.isSubmitting.set(false);
  }

  publishExam(examId: string) {
    Swal.fire({
      title: this.translate.instant('COURSE_EXAMS.SWAL_PUBLISH_TITLE'),
      text: this.translate.instant('COURSE_EXAMS.SWAL_PUBLISH_TEXT'),
      icon: 'info',
      showCancelButton: true,
      confirmButtonColor: '#215DAE', // Matches your primary color
      cancelButtonColor: '#87949C',
      confirmButtonText: this.translate.instant('COURSE_EXAMS.SWAL_BTN_PUBLISH')
    }).then((result) => {
      if (result.isConfirmed) {
        this.examService.publishExam(examId).subscribe({
          next: () => {
            this.toastr.success(this.translate.instant('COURSE_EXAMS.TOAST_PUBLISH_SUCCESS'));
            this.loadExams();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('COURSE_EXAMS.TOAST_PUBLISH_ERR'));
          }
        });
      }
    });
  }

  deleteExam(examId: string) {
    Swal.fire({
      title: this.translate.instant('COURSE_EXAMS.SWAL_DEL_TITLE'),
      text: this.translate.instant('COURSE_EXAMS.SWAL_DEL_TEXT'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444', // Matches your danger color
      cancelButtonColor: '#87949C',
      confirmButtonText: this.translate.instant('COURSE_EXAMS.SWAL_BTN_DEL')
    }).then((result) => {
      if (result.isConfirmed) {
        this.examService.deleteExam(examId).subscribe({
          next: () => {
            this.toastr.success(this.translate.instant('COURSE_EXAMS.TOAST_DEL_SUCCESS'));
            this.loadExams();
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('COURSE_EXAMS.TOAST_DEL_ERR'));
          }
        });
      }
    });
  }

  canManageExams(): boolean {
    const role = this.authService.userRole();
    if (role === 'Admin') return true;
    if (role === 'Tutor' && this.course.assignedTutorId === this.authService.currentUser()?.userId) return true;
    return false;
  }

  isExamScheduled(exam: ExamSummaryResponse): boolean {
    if (exam.status !== 'Published') return false;
    if (!exam.scheduledAt) return false;
    return new Date(exam.scheduledAt) > this.currentTime();
  }

  /**
   * Exam is "Active" when it is Published AND the window [scheduledAt, scheduledEndAt] is
   * currently open (or no window is configured at all).
   * It also checks if the student has an approved Re-open Request that hasn't expired yet.
   */
  isExamActive(exam: ExamSummaryResponse): boolean {
    if (exam.status !== 'Published') return false;
    const now = this.currentTime();
    
    // Check for any approved request extension (Re-open or Re-attempt)
    const req = this.myRequests().find(r => r.examId === exam.id && r.status === 'Approved');
    if (req && req.grantedExtensionUntil) {
      if (new Date(req.grantedExtensionUntil) > now) {
        return true; // Still active for this student due to extension
      }
    }

    if (exam.scheduledAt && new Date(exam.scheduledAt) > now) return false; // not started yet
    if (exam.scheduledEndAt && new Date(exam.scheduledEndAt) < now) return false; // already over
    return true;
  }

  isExamExpired(exam: ExamSummaryResponse): boolean {
    const now = this.currentTime();
    
    // Check for any approved request extension (Re-open or Re-attempt)
    const req = this.myRequests().find(r => r.examId === exam.id && r.status === 'Approved');
    if (req && req.grantedExtensionUntil) {
      if (new Date(req.grantedExtensionUntil) > now) {
        return false; // Not expired yet because they have an active extension
      }
    }

    if (!exam.scheduledEndAt) return false;
    return new Date(exam.scheduledEndAt) < now;
  }

  /** True when editing an exam that is currently expired. */
  isEditingExpiredExam(): boolean {
    const exam = this.editingExam();
    if (!exam) return false;
    // For editing, tutors only care about the global exam schedule, not student extensions
    if (!exam.scheduledEndAt) return false;
    return new Date(exam.scheduledEndAt) < this.currentTime();
  }

  takeExam(examId: string) {
    this.router.navigate(['/exam-engine', examId]);
  }

  viewResults(examId: string, latestAttemptId: string) {
    this.examResultService.getStudentAttempts(examId).subscribe({
      next: (res) => {
        const attempts = res.data || [];
        if (attempts.length > 1) {
          this.studentAttemptsForPicker.set(attempts);
          this.pickerExamId.set(examId);
          this.showAttemptPicker.set(true);
          setTimeout(() => {
            const el = this.document.querySelector('.attempt-picker-overlay');
            if (el && el.parentElement !== this.document.body) {
              this.renderer.appendChild(this.document.body, el);
              requestAnimationFrame(() => {
                this.renderer.removeClass(el, 'modal-pending');
              });
            }
          }, 0);
        } else {
          // Fallback to directly viewing the latest attempt if only 1 exists
          this.router.navigate(['/exam-results', latestAttemptId]);
        }
      },
      error: () => {
        this.toastr.error(this.translate.instant('COURSE_EXAMS.TOAST_LOAD_ATTEMPTS_ERR'));
        this.router.navigate(['/exam-results', latestAttemptId]);
      }
    });
  }

  viewSpecificAttempt(attemptId: string) {
    this.closeAttemptPicker(() => {
      this.router.navigate(['/exam-results', attemptId]);
    });
  }

  closeAttemptPicker(callback?: () => void) {
    const el = this.document.querySelector('.attempt-picker-overlay');
    if (el) this.renderer.addClass(el, 'modal-pending');
    setTimeout(() => {
      if (el && el.parentElement === this.document.body) {
        this.renderer.removeChild(this.document.body, el);
      }
      this.showAttemptPicker.set(false);
      this.pickerExamId.set(null);
      if (callback) callback();
    }, 150);
  }


  manageResults(examId: string) {
    this.router.navigate(['/courses', this.course.id, 'exams', examId, 'results']);
  }

  getExamQuestionCount(exam: ExamSummaryResponse): number {
    if (!exam.selectionRules) return 0;
    return exam.selectionRules.reduce((sum, rule) => sum + rule.count, 0);
  }

  getReopenRequestStatus(examId: string): string | null {
    const req = this.myRequests().find(r => r.examId === examId);
    return req ? req.status : null;
  }

  requestReopen(examId: string) {
    Swal.fire({
      title: this.translate.instant('COURSE_EXAMS.SWAL_REOPEN_TITLE'),
      html: `
        <div style="text-align: left; padding-top: 10px;">
          <div style="display: flex; gap: 12px; margin-bottom: 20px; padding: 12px 16px; background: rgba(33, 93, 174, 0.08); border-radius: 10px; border-left: 4px solid #215DAE;">
             <svg style="flex-shrink: 0; margin-top: 2px;" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#215DAE" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><path d="M12 16v-4"></path><path d="M12 8h.01"></path></svg>
             <p style="margin: 0; font-size: 14px; color: var(--theme-text-main); line-height: 1.5;">${this.translate.instant('COURSE_EXAMS.SWAL_REOPEN_TEXT')}</p>
          </div>
          
          <div style="margin-bottom: 20px;">
            <label style="display: block; font-size: 13px; font-weight: 600; color: var(--theme-text-secondary); margin-bottom: 8px;">Justification / Reason *</label>
            <textarea id="swal-input-justification" placeholder="${this.translate.instant('COURSE_EXAMS.SWAL_REOPEN_PLACEHOLDER')}" style="width: 100%; height: 110px; padding: 14px; border: 1px solid var(--theme-border); border-radius: 10px; background: var(--theme-bg-secondary); color: var(--theme-text-main); font-family: inherit; font-size: 14px; resize: none; outline: none; transition: all 0.2s ease;" onfocus="this.style.borderColor='#215DAE'; this.style.boxShadow='0 0 0 4px rgba(33, 93, 174, 0.1)'" onblur="this.style.borderColor='var(--theme-border)'; this.style.boxShadow='none'"></textarea>
          </div>
          
          <div>
             <label style="display: block; font-size: 13px; font-weight: 600; color: var(--theme-text-secondary); margin-bottom: 8px;">${this.translate.instant('COURSE_EXAMS.SWAL_REOPEN_PROOF')}</label>
             <div style="position: relative; display: flex; flex-direction: column; align-items: center; justify-content: center; border: 2px dashed var(--theme-border); border-radius: 10px; padding: 20px; background: var(--theme-bg-secondary); transition: all 0.2s ease;" onmouseover="this.style.borderColor='#215DAE'; this.style.background='rgba(33, 93, 174, 0.02)'" onmouseout="this.style.borderColor='var(--theme-border)'; this.style.background='var(--theme-bg-secondary)'">
               <svg style="margin-bottom: 10px; color: var(--theme-text-secondary);" xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
               <input type="file" id="swal-input-file" style="width: 100%; font-size: 13px; color: var(--theme-text-secondary); cursor: pointer;" accept="image/*,.pdf,.doc,.docx" />
             </div>
          </div>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: this.translate.instant('COURSE_EXAMS.SWAL_BTN_SUBMIT_REQ'),
      cancelButtonText: this.translate.instant('EXAM_RESULT_PAGE.SWAL_BTN_CANCEL'),
      confirmButtonColor: '#215DAE',
      cancelButtonColor: '#87949C',
      background: 'var(--theme-bg-main)',
      color: 'var(--theme-text-main)',
      preConfirm: () => {
        const justification = (document.getElementById('swal-input-justification') as HTMLTextAreaElement).value;
        const fileInput = document.getElementById('swal-input-file') as HTMLInputElement;
        
        if (!justification || justification.trim().length < 20) {
          Swal.showValidationMessage(this.translate.instant('COURSE_EXAMS.SWAL_REOPEN_ERR_MIN'));
          return false;
        }

        let file: File | undefined;
        if (fileInput.files && fileInput.files.length > 0) {
          file = fileInput.files[0];
          if (file.size > 10 * 1024 * 1024) { // 10MB
            Swal.showValidationMessage(this.translate.instant('COURSE_EXAMS.SWAL_REOPEN_ERR_SIZE'));
            return false;
          }
        }

        return { justification: justification.trim(), file };
      }
    }).then((swalResult) => {
      if (swalResult.isConfirmed && swalResult.value) {
        const { justification, file } = swalResult.value;
        // isReopenRequest = true is not explicitly strongly typed in our generated frontend client, 
        // but our backend endpoint accepts it as a form field. Since our submitRequest doesn't 
        // have an isReopenRequest param, we need to adapt our service call. Let's add it to the FormData.
        
        // Actually, we need to pass true for isReopenRequest. Let's temporarily call it manually or update the service.
        // I will just update ReattemptService to accept it. 
        this.reattemptService.submitRequest(examId, { justification, isReopenRequest: true } as any, file).subscribe({
          next: (res) => {
            this.toastr.success(res.message);
            this.loadExams(); // reload to get updated requests
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('EXAM_RESULT_PAGE.TOAST_REQ_ERR'));
          }
        });
      }
    });
  }
}
