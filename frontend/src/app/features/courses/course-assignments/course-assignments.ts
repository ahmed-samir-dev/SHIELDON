import {
  Component, Input, OnInit, OnDestroy, inject, signal, computed
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  AssignmentService,
  AssignmentResponse,
  AssignmentSubmissionResponse
} from '../services/assignment.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-course-assignments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: './course-assignments.html',
  styleUrls: ['./course-assignments.scss']
})
export class CourseAssignmentsComponent implements OnInit, OnDestroy {
  @Input() course!: CourseDetailResponse;

  private readonly assignmentService = inject(AssignmentService);
  private readonly authService       = inject(AuthService);
  private readonly fb                = inject(FormBuilder);
  private readonly toastr            = inject(ToastrService);

  // ── State ──────────────────────────────────────────────────────────────
  assignments      = signal<AssignmentResponse[]>([]);
  isLoading        = signal(true);
  isSubmitting     = signal(false);
  now              = signal(Date.now());
  private timer?: ReturnType<typeof setInterval>;

  // Modals
  showCreateModal  = signal(false);
  showEditModal    = signal(false);
  showSubmitModal  = signal(false);

  // Selected assignment context
  selectedAssignment = signal<AssignmentResponse | null>(null);
  expandedSubmissions = signal<Set<string>>(new Set());
  submissionsMap   = signal<Map<string, AssignmentSubmissionResponse[]>>(new Map());
  loadingSubmissions = signal<Set<string>>(new Set());
  downloadingZip   = signal<Set<string>>(new Set());

  // File inputs
  selectedReferenceFile: File | null = null;
  selectedSubmissionFile: File | null = null;
  isDragging = signal(false);

  // Forms
  createForm!: FormGroup;
  editForm!: FormGroup;

  // ── Computed roles ─────────────────────────────────────────────────────
  canManage = computed(() => {
    if (this.authService.isAdmin()) return true;
    if (this.authService.isTutor() && this.course.assignedTutorId === this.authService.currentUser()?.userId) return true;
    return false;
  });

  isStudent = computed(() => this.authService.isStudent());

  ngOnInit(): void {
    this.loadAssignments();
    this.initForms();
    this.timer = setInterval(() => this.now.set(Date.now()), 1000);
  }

  ngOnDestroy(): void {
    if (this.timer) {
      clearInterval(this.timer);
    }
  }

  private initForms(): void {
    this.createForm = this.fb.group({
      title:        ['', [Validators.required, Validators.maxLength(300)]],
      instructions: ['', Validators.maxLength(3000)],
      dueDate:      [null],
      weight:       [0, [Validators.required, Validators.min(0), Validators.max(100)]]
    });

    this.editForm = this.fb.group({
      title:        ['', [Validators.required, Validators.maxLength(300)]],
      instructions: ['', Validators.maxLength(3000)],
      dueDate:      [null],
      weight:       [0, [Validators.required, Validators.min(0), Validators.max(100)]]
    });
  }

  // ── Load ───────────────────────────────────────────────────────────────

  loadAssignments(): void {
    this.isLoading.set(true);
    this.assignmentService.getAssignments(this.course.id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.assignments.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load assignments');
        this.isLoading.set(false);
      }
    });
  }

  // ── Create Assignment ──────────────────────────────────────────────────

  openCreateModal(): void {
    this.createForm.reset();
    this.selectedReferenceFile = null;
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    this.showCreateModal.set(false);
    this.selectedReferenceFile = null;
  }

  onReferenceFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedReferenceFile = input.files?.[0] ?? null;
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(): void {
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent, target: 'reference' | 'submission'): void {
    event.preventDefault();
    this.isDragging.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (!file) return;
    if (target === 'reference') this.selectedReferenceFile = file;
    else this.selectedSubmissionFile = file;
  }

  submitCreateForm(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const formData = new FormData();
    formData.append('title', this.createForm.value.title.trim());
    if (this.createForm.value.instructions) {
      formData.append('instructions', this.createForm.value.instructions.trim());
    }
    if (this.createForm.value.dueDate) {
      formData.append('dueDate', new Date(this.createForm.value.dueDate).toISOString());
    }
    formData.append('weight', this.createForm.value.weight.toString());
    if (this.selectedReferenceFile) {
      formData.append('referenceFile', this.selectedReferenceFile);
    }

    this.assignmentService.createAssignment(this.course.id, formData).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.toastr.success('Assignment created successfully!');
          this.assignments.update(list => [res.data!, ...list]);
          this.closeCreateModal();
        }
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to create assignment');
        this.isSubmitting.set(false);
      }
    });
  }

  // ── Edit Assignment ────────────────────────────────────────────────────

  openEditModal(assignment: AssignmentResponse): void {
    this.selectedAssignment.set(assignment);
    this.editForm.patchValue({
      title:        assignment.title,
      instructions: assignment.instructions,
      dueDate:      assignment.dueDate ? this.toDatetimeLocalFormat(assignment.dueDate) : null,
      weight:       assignment.weight
    });
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    this.showEditModal.set(false);
    this.selectedAssignment.set(null);
  }

  submitEditForm(): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const assignment = this.selectedAssignment();
    if (!assignment) return;

    this.isSubmitting.set(true);
    const request = {
      title:        this.editForm.value.title.trim(),
      instructions: this.editForm.value.instructions?.trim() || null,
      dueDate:      this.editForm.value.dueDate ? new Date(this.editForm.value.dueDate).toISOString() : null,
      weight:       this.editForm.value.weight
    };

    this.assignmentService.updateAssignment(this.course.id, assignment.id, request).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.toastr.success('Assignment updated successfully!');
          this.assignments.update(list =>
            list.map(a => a.id === assignment.id ? res.data! : a)
          );
          this.closeEditModal();
        }
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to update assignment');
        this.isSubmitting.set(false);
      }
    });
  }

  // ── Delete Assignment ──────────────────────────────────────────────────

  deleteAssignment(assignmentId: string): void {
    Swal.fire({
      title: 'Delete Assignment?',
      text: 'This will permanently delete the assignment and ALL student submissions. This action cannot be undone.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, delete it'
    }).then(result => {
      if (result.isConfirmed) {
        this.assignmentService.deleteAssignment(this.course.id, assignmentId).subscribe({
          next: () => {
            this.toastr.success('Assignment deleted successfully');
            this.assignments.update(list => list.filter(a => a.id !== assignmentId));
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to delete assignment');
          }
        });
      }
    });
  }

  // ── Reference File Download ────────────────────────────────────────────

  downloadReference(assignment: AssignmentResponse): void {
    if (!assignment.referenceFileName) return;
    this.assignmentService.downloadReferenceFile(this.course.id, assignment.id, assignment.referenceFileName);
  }

  // ── Submissions expansion (Tutor/Admin) ───────────────────────────────

  toggleSubmissions(assignmentId: string): void {
    const expanded = this.expandedSubmissions();
    const next = new Set(expanded);
    if (next.has(assignmentId)) {
      next.delete(assignmentId);
    } else {
      next.add(assignmentId);
      this.loadSubmissionsIfNeeded(assignmentId);
    }
    this.expandedSubmissions.set(next);
  }

  isSubmissionsExpanded(assignmentId: string): boolean {
    return this.expandedSubmissions().has(assignmentId);
  }

  private loadSubmissionsIfNeeded(assignmentId: string): void {
    if (this.submissionsMap().has(assignmentId)) return;

    const loading = new Set(this.loadingSubmissions());
    loading.add(assignmentId);
    this.loadingSubmissions.set(loading);

    this.assignmentService.getSubmissions(this.course.id, assignmentId).subscribe({
      next: (res) => {
        const map = new Map(this.submissionsMap());
        map.set(assignmentId, res.data ?? []);
        this.submissionsMap.set(map);
        const l = new Set(this.loadingSubmissions());
        l.delete(assignmentId);
        this.loadingSubmissions.set(l);
      },
      error: () => {
        const l = new Set(this.loadingSubmissions());
        l.delete(assignmentId);
        this.loadingSubmissions.set(l);
      }
    });
  }

  getSubmissionsForAssignment(assignmentId: string): AssignmentSubmissionResponse[] {
    return this.submissionsMap().get(assignmentId) ?? [];
  }

  isLoadingSubmissions(assignmentId: string): boolean {
    return this.loadingSubmissions().has(assignmentId);
  }

  // ── Download Submission / ZIP ──────────────────────────────────────────

  downloadSub(assignment: AssignmentResponse, sub: AssignmentSubmissionResponse): void {
    this.assignmentService.downloadSubmission(
      this.course.id, assignment.id, sub.id, sub.originalFileName
    );
  }

  downloadAllZip(assignment: AssignmentResponse): void {
    const downloading = new Set(this.downloadingZip());
    downloading.add(assignment.id);
    this.downloadingZip.set(downloading);

    const zipName = `${this.course.courseCode}_${assignment.title.replace(/\s+/g, '_')}_submissions.zip`;
    this.assignmentService.downloadAllSubmissionsAsZip(this.course.id, assignment.id, zipName);

    setTimeout(() => {
      const d = new Set(this.downloadingZip());
      d.delete(assignment.id);
      this.downloadingZip.set(d);
    }, 2000);
  }

  isDownloadingZip(assignmentId: string): boolean {
    return this.downloadingZip().has(assignmentId);
  }

  deleteSubmission(assignment: AssignmentResponse, submissionId: string): void {
    Swal.fire({
      title: 'Delete Submission?',
      text: 'This will permanently remove this student\'s submission.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, delete it'
    }).then(result => {
      if (result.isConfirmed) {
        this.assignmentService.deleteSubmission(this.course.id, assignment.id, submissionId).subscribe({
          next: () => {
            this.toastr.success('Submission deleted');
            const map = new Map(this.submissionsMap());
            const subs = (map.get(assignment.id) ?? []).filter(s => s.id !== submissionId);
            map.set(assignment.id, subs);
            this.submissionsMap.set(map);
            this.assignments.update(list =>
              list.map(a => a.id === assignment.id
                ? { ...a, submissionCount: a.submissionCount - 1 }
                : a)
            );
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to delete submission');
          }
        });
      }
    });
  }

  gradeSubmission(assignment: AssignmentResponse, submission: AssignmentSubmissionResponse): void {
    Swal.fire({
      title: 'Score Submission',
      html: `
        <div class="swal-form-group">
          <label>Score Awarded (Max: 100)</label>
          <input id="swal-points" class="swal2-input" type="number" step="0.5" value="${submission.pointsAwarded ?? ''}" min="0" max="100">
        </div>
        <div class="swal-form-group">
          <label>Feedback (Optional)</label>
          <textarea id="swal-feedback" class="swal2-textarea" placeholder="Add feedback...">${submission.feedback || ''}</textarea>
        </div>
      `,
      showCancelButton: true,
      confirmButtonText: 'Save Score',
      preConfirm: () => {
        const pointsVal = (document.getElementById('swal-points') as HTMLInputElement).value;
        const feedbackVal = (document.getElementById('swal-feedback') as HTMLTextAreaElement).value;
        
        if (!pointsVal) {
          Swal.showValidationMessage('Score awarded is required');
          return null;
        }

        return {
          pointsAwarded: parseFloat(pointsVal),
          feedback: feedbackVal
        };
      }
    }).then((result) => {
      if (result.isConfirmed && result.value) {
        this.assignmentService.reviewSubmission(this.course.id, assignment.id, submission.id, result.value).subscribe({
          next: (res) => {
            if (res.success && res.data) {
              this.toastr.success('Submission scored successfully!');
              
              // Update submission locally
              const map = new Map(this.submissionsMap());
              const subs = map.get(assignment.id) || [];
              const updatedSubs = subs.map(s => s.id === submission.id ? res.data! : s);
              map.set(assignment.id, updatedSubs);
              this.submissionsMap.set(map);
            }
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to grade submission');
          }
        });
      }
    });
  }

  // ── Student submission ─────────────────────────────────────────────────

  openSubmitModal(assignment: AssignmentResponse): void {
    this.selectedAssignment.set(assignment);
    this.selectedSubmissionFile = null;
    this.showSubmitModal.set(true);
  }

  closeSubmitModal(): void {
    this.showSubmitModal.set(false);
    this.selectedAssignment.set(null);
    this.selectedSubmissionFile = null;
  }

  onSubmissionFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedSubmissionFile = input.files?.[0] ?? null;
  }

  submitMyWork(): void {
    const assignment = this.selectedAssignment();
    if (!assignment || !this.selectedSubmissionFile) {
      this.toastr.warning('Please select a file to submit.');
      return;
    }

    this.isSubmitting.set(true);
    const formData = new FormData();
    formData.append('submissionFile', this.selectedSubmissionFile);

    this.assignmentService.submitAssignment(this.course.id, assignment.id, formData).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.toastr.success('Assignment submitted successfully!');
          this.assignments.update(list =>
            list.map(a => a.id === assignment.id
              ? { ...a, mySubmission: res.data!, submissionCount: a.submissionCount + 1 }
              : a)
          );
          this.closeSubmitModal();
        }
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to submit assignment');
        this.isSubmitting.set(false);
      }
    });
  }

  deleteMySubmission(assignment: AssignmentResponse): void {
    Swal.fire({
      title: 'Remove Your Submission?',
      text: 'You can re-submit a new file afterwards (while the deadline is open).',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, remove it'
    }).then(result => {
      if (result.isConfirmed && assignment.mySubmission) {
        this.assignmentService.deleteSubmission(
          this.course.id, assignment.id, assignment.mySubmission.id
        ).subscribe({
          next: () => {
            this.toastr.success('Your submission was removed');
            this.assignments.update(list =>
              list.map(a => a.id === assignment.id
                ? { ...a, mySubmission: null, submissionCount: a.submissionCount - 1 }
                : a)
            );
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to remove submission');
          }
        });
      }
    });
  }

  downloadMySubmission(assignment: AssignmentResponse): void {
    if (!assignment.mySubmission) return;
    this.assignmentService.downloadSubmission(
      this.course.id, assignment.id,
      assignment.mySubmission.id,
      assignment.mySubmission.originalFileName
    );
  }

  // ── Utility helpers ────────────────────────────────────────────────────

  getDueBadgeClass(assignment: AssignmentResponse): string {
    if (!assignment.dueDate) return '';
    if (assignment.isPastDue) return 'badge-past-due';
    const diff = new Date(assignment.dueDate).getTime() - this.now();
    if (diff <= 0) return 'badge-past-due';
    const hours = diff / (1000 * 60 * 60);
    return hours < 24 ? 'badge-due-soon' : 'badge-due';
  }

  getDueLabel(assignment: AssignmentResponse): string {
    if (!assignment.dueDate) return '';
    if (assignment.isPastDue) return 'Past Due';
    
    let diff = new Date(assignment.dueDate).getTime() - this.now();
    if (diff <= 0) return 'Past Due';

    const days  = Math.floor(diff / (1000 * 60 * 60 * 24));
    diff -= days * (1000 * 60 * 60 * 24);
    
    const hours = Math.floor(diff / (1000 * 60 * 60));
    diff -= hours * (1000 * 60 * 60);
    
    const mins = Math.floor(diff / (1000 * 60));
    diff -= mins * (1000 * 60);
    
    const secs = Math.floor(diff / 1000);
    
    if (days > 0) return `Due in ${days}d ${hours}h ${mins}m ${secs}s`;
    if (hours > 0) return `Due in ${hours}h ${mins}m ${secs}s`;
    if (mins > 0) return `Due in ${mins}m ${secs}s`;
    return `Due in ${secs}s`;
  }

  formatBytes(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  private toDatetimeLocalFormat(isoString: string): string {
    const d = new Date(isoString);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }
}
