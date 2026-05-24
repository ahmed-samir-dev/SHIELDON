import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ExamResultService, ExamResultResponse, ExamAttemptSummaryDto } from '../services/exam-result';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, CheckCircle, XCircle, ArrowLeft, Clock, FileText, Check, X, AlertCircle } from 'lucide-angular';
import confetti from 'canvas-confetti';
import Swal from 'sweetalert2';
import { ReattemptService, StudentReattemptStatusResponse } from '../services/reattempt.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-exam-result-page',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, TranslateModule],
  templateUrl: './exam-result-page.html',
  styleUrl: './exam-result-page.scss'
})
export class ExamResultPage implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private examResultService = inject(ExamResultService);
  private reattemptService = inject(ReattemptService);
  private toastr = inject(ToastrService);
  private translate = inject(TranslateService);

  // Icons
  readonly CheckCircle = CheckCircle;
  readonly XCircle = XCircle;
  readonly ArrowLeft = ArrowLeft;
  readonly Clock = Clock;
  readonly FileText = FileText;
  readonly Check = Check;
  readonly X = X;
  readonly AlertCircle = AlertCircle;

  isLoading = signal(true);
  result = signal<ExamResultResponse | null>(null);
  studentAttempts = signal<ExamAttemptSummaryDto[]>([]);
  showAttemptPicker = signal(false);
  
  existingReattemptRequest = signal<StudentReattemptStatusResponse | null>(null);
  
  // Circle animation state
  circumference = 2 * Math.PI * 70; // r=70
  dashOffset = signal(this.circumference);
  
  private animationTimeout: any;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const attemptId = params.get('attemptId');
      if (!attemptId) {
        this.toastr.error(this.translate.instant('EXAM_RESULT_PAGE.TOAST_ERR_INVALID_ID'));
        this.router.navigate(['/courses']);
        return;
      }
      this.loadResult(attemptId);
    });
  }

  ngOnDestroy() {
    if (this.animationTimeout) clearTimeout(this.animationTimeout);
  }

  loadResult(attemptId: string) {
    this.isLoading.set(true);
    this.dashOffset.set(this.circumference); // Reset circle animation
    
    this.examResultService.getAttemptResult(attemptId).subscribe({
      next: (res) => {
        this.result.set(res.data);
        this.isLoading.set(false);
        
        if (res.data.resultVisible && res.data.score !== null) {
          this.animateScore(res.data.score);
          if (res.data.passed) {
            this.triggerConfetti();
          }
        }
        
        // Fetch all attempts for this student for this exam
        this.examResultService.getStudentAttempts(res.data.examId).subscribe({
          next: (attemptsRes) => this.studentAttempts.set(attemptsRes.data),
          error: () => console.error('Failed to load student attempts')
        });

        // Fetch existing re-attempt requests for this exam
        this.loadExistingReattemptRequest(res.data.examId);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || this.translate.instant('EXAM_RESULT_PAGE.TOAST_ERR_LOAD'));
        this.isLoading.set(false);
      }
    });
  }

  loadExistingReattemptRequest(examId: string) {
    this.reattemptService.getMyRequests().subscribe({
      next: (res) => {
        const req = res.data.find(r => r.examId === examId);
        this.existingReattemptRequest.set(req || null);
      },
      error: () => console.error('Failed to load re-attempt requests')
    });
  }

  animateScore(score: number) {
    // Slight delay to allow DOM to render
    this.animationTimeout = setTimeout(() => {
      const offset = this.circumference - (score / 100) * this.circumference;
      this.dashOffset.set(offset);
    }, 100);
  }

  triggerConfetti() {
    const count = 200;
    const defaults = {
      origin: { y: 0.7 }
    };

    function fire(particleRatio: number, opts: any) {
      confetti({
        ...defaults,
        ...opts,
        particleCount: Math.floor(count * particleRatio)
      });
    }

    fire(0.25, {
      spread: 26,
      startVelocity: 55,
    });
    fire(0.2, {
      spread: 60,
    });
    fire(0.35, {
      spread: 100,
      decay: 0.91,
      scalar: 0.8
    });
    fire(0.1, {
      spread: 120,
      startVelocity: 25,
      decay: 0.92,
      scalar: 1.2
    });
    fire(0.1, {
      spread: 120,
      startVelocity: 45,
    });
  }

  getScoreColorClass(): string {
    const score = this.result()?.score ?? 0;
    const passScore = this.result()?.passScore ?? 50;
    return score >= passScore ? 'score-pass' : 'score-fail';
  }

  goBack() {
    const courseId = this.result()?.courseId; 
    this.result.set(null);
    this.router.navigate(['/courses', courseId], { queryParams: { tab: 'exams' } });
  }

  viewAttempt(attemptId: string) {
    this.showAttemptPicker.set(false);
    this.router.navigate(['/exam-results', attemptId]);
  }

  openAttemptPicker() {
    this.showAttemptPicker.set(true);
  }

  closeAttemptPicker() {
    this.showAttemptPicker.set(false);
  }

  requestReattempt() {
    const examId = this.result()?.examId;
    if (!examId) return;

    Swal.fire({
      title: this.translate.instant('EXAM_RESULT_PAGE.SWAL_REQ_TITLE'),
      html: `
        <div style="text-align: left; padding-top: 10px;">
          <div style="display: flex; gap: 12px; margin-bottom: 20px; padding: 12px 16px; background: rgba(33, 93, 174, 0.08); border-radius: 10px; border-left: 4px solid #215DAE;">
             <svg style="flex-shrink: 0; margin-top: 2px;" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#215DAE" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><path d="M12 16v-4"></path><path d="M12 8h.01"></path></svg>
             <p style="margin: 0; font-size: 14px; color: var(--theme-text-main); line-height: 1.5;">${this.translate.instant('EXAM_RESULT_PAGE.SWAL_REQ_DESC')}</p>
          </div>
          
          <div style="margin-bottom: 20px;">
            <label style="display: block; font-size: 13px; font-weight: 600; color: var(--theme-text-secondary); margin-bottom: 8px;">Justification / Reason *</label>
            <textarea id="swal-input-justification" placeholder="${this.translate.instant('EXAM_RESULT_PAGE.SWAL_PLACEHOLDER')}" style="width: 100%; height: 110px; padding: 14px; border: 1px solid var(--theme-border); border-radius: 10px; background: var(--theme-bg-secondary); color: var(--theme-text-main); font-family: inherit; font-size: 14px; resize: none; outline: none; transition: all 0.2s ease;" onfocus="this.style.borderColor='#215DAE'; this.style.boxShadow='0 0 0 4px rgba(33, 93, 174, 0.1)'" onblur="this.style.borderColor='var(--theme-border)'; this.style.boxShadow='none'"></textarea>
          </div>
          
          <div>
             <label style="display: block; font-size: 13px; font-weight: 600; color: var(--theme-text-secondary); margin-bottom: 8px;">Proof Attachment (Optional, max 10MB)</label>
             <div style="position: relative; display: flex; flex-direction: column; align-items: center; justify-content: center; border: 2px dashed var(--theme-border); border-radius: 10px; padding: 20px; background: var(--theme-bg-secondary); transition: all 0.2s ease;" onmouseover="this.style.borderColor='#215DAE'; this.style.background='rgba(33, 93, 174, 0.02)'" onmouseout="this.style.borderColor='var(--theme-border)'; this.style.background='var(--theme-bg-secondary)'">
               <svg style="margin-bottom: 10px; color: var(--theme-text-secondary);" xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
               <input type="file" id="swal-input-file" style="width: 100%; font-size: 13px; color: var(--theme-text-secondary); cursor: pointer;" accept="image/*,.pdf,.doc,.docx" />
             </div>
          </div>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: this.translate.instant('EXAM_RESULT_PAGE.SWAL_BTN_SUBMIT'),
      cancelButtonText: this.translate.instant('EXAM_RESULT_PAGE.SWAL_BTN_CANCEL'),
      confirmButtonColor: '#215DAE',
      cancelButtonColor: '#87949C',
      preConfirm: () => {
        const justification = (document.getElementById('swal-input-justification') as HTMLTextAreaElement).value;
        const fileInput = document.getElementById('swal-input-file') as HTMLInputElement;
        
        if (!justification || justification.trim().length < 20) {
          Swal.showValidationMessage(this.translate.instant('EXAM_RESULT_PAGE.SWAL_VAL_ERR'));
          return false;
        }

        let file: File | undefined;
        if (fileInput.files && fileInput.files.length > 0) {
          file = fileInput.files[0];
          if (file.size > 10 * 1024 * 1024) { // 10MB
            Swal.showValidationMessage('File size must not exceed 10MB.');
            return false;
          }
        }

        return { justification: justification.trim(), file };
      }
    }).then((swalResult) => {
      if (swalResult.isConfirmed && swalResult.value) {
        const { justification, file } = swalResult.value;
        this.reattemptService.submitRequest(examId, { justification }, file).subscribe({
          next: (res) => {
            this.toastr.success(res.message);
            this.existingReattemptRequest.set(res.data);
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('EXAM_RESULT_PAGE.TOAST_REQ_ERR'));
          }
        });
      }
    });
  }

  getImageUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (url.startsWith('http')) return url;
    const apiUrl = environment.apiUrl.replace('/api', '');
    return `${apiUrl}/${url}`;
  }
}
