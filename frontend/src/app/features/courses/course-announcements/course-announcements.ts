import { Component, Input, OnInit, OnDestroy, inject, signal, computed, ViewChild, ElementRef, Renderer2 } from '@angular/core';
import { CommonModule, DatePipe, DOCUMENT } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AnnouncementService, AnnouncementResponse } from '../services/announcement.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { ToastrService } from 'ngx-toastr';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import Swal from 'sweetalert2';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-course-announcements',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, TranslateModule],
  templateUrl: './course-announcements.html',
  styleUrls: ['./course-announcements.scss']
})
export class CourseAnnouncementsComponent implements OnInit, OnDestroy {
  @Input() course!: CourseDetailResponse;

  private readonly translate = inject(TranslateService);
  private readonly announcementService = inject(AnnouncementService);
  private readonly authService = inject(AuthService);
  private readonly languageService = inject(LanguageService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toastr = inject(ToastrService);
  private readonly renderer = inject(Renderer2);
  private readonly document = inject(DOCUMENT);
  private langSub!: Subscription;

  @ViewChild('postModalOverlay') postModalOverlayRef!: ElementRef<HTMLElement>;

  announcements = signal<AnnouncementResponse[]>([]);
  isLoading = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);
  showPostForm = signal<boolean>(false);

  // ── Drag & Drop / Reordering State ──────────────────────────────────────────
  isReordering = signal<boolean>(false);
  hasUnsavedOrder = signal<boolean>(false);
  isSavingOrder = signal<boolean>(false);
  draggedIndex = signal<number | null>(null);
  dragOverIndex = signal<number | null>(null);
  private originalAnnouncementsSnapshot: AnnouncementResponse[] = [];

  postForm!: FormGroup;

  canManageAnnouncements = computed(() => {
    if (this.authService.isAdmin()) return true;
    if (this.authService.isTutor() && this.course.assignedTutorId === this.authService.currentUser()?.userId) return true;
    return false;
  });

  ngOnInit(): void {
    this.loadAnnouncements();
    this.initForm();
    // Auto-reload when user toggles language
    this.langSub = this.languageService.languageChange$.subscribe(() => this.loadAnnouncements());
  }

  ngOnDestroy(): void {
    this.langSub?.unsubscribe();
  }

  private initForm(): void {
    this.postForm = this.formBuilder.group({
      title: ['', [Validators.required, Validators.maxLength(300)]],
      content: ['', [Validators.required, Validators.maxLength(5000)]],
      priority: ['Normal', Validators.required]
    });
  }

  loadAnnouncements(): void {
    this.isLoading.set(true);
    this.announcementService.getAnnouncements(this.course.id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.announcements.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || this.translate.instant('COURSE_ANNOUNCEMENTS.TOAST_LOAD_ERR'));
        this.isLoading.set(false);
      }
    });
  }

  togglePostForm(): void {
    if (!this.showPostForm()) {
      this.showPostForm.set(true);
      setTimeout(() => {
        const el = this.postModalOverlayRef?.nativeElement;
        if (el) {
          this.renderer.appendChild(this.document.body, el);
          requestAnimationFrame(() => {
            this.renderer.removeClass(el, 'modal-pending');
          });
        }
      }, 0);
    } else {
      const el = this.postModalOverlayRef?.nativeElement;
      if (el) this.renderer.addClass(el, 'modal-pending');
      setTimeout(() => {
        if (el && el.parentElement === this.document.body) {
          this.renderer.removeChild(this.document.body, el);
        }
        this.showPostForm.set(false);
        this.postForm.reset({ priority: 'Normal' });
      }, 150);
    }
  }

  onSubmit(): void {
    if (this.postForm.invalid) {
      this.postForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    this.announcementService.createAnnouncement(this.course.id, this.postForm.value).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.toastr.success(this.translate.instant('COURSE_ANNOUNCEMENTS.TOAST_POST_SUCCESS'));
          this.announcements.update(list => {
            if (res.data!.priority === 'Important') return [res.data!, ...list];
            const firstNormal = list.findIndex(a => a.priority === 'Normal');
            if (firstNormal === -1) return [...list, res.data!];
            return [...list.slice(0, firstNormal), res.data!, ...list.slice(firstNormal)];
          });
          this.togglePostForm();
        }
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || this.translate.instant('COURSE_ANNOUNCEMENTS.TOAST_POST_ERR'));
        this.isSubmitting.set(false);
      }
    });
  }

  deleteAnnouncement(announcementId: string): void {
    Swal.fire({
      title: this.translate.instant('COURSE_ANNOUNCEMENTS.SWAL_DELETE_TITLE'),
      text: this.translate.instant('COURSE_ANNOUNCEMENTS.SWAL_DELETE_DESC'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: this.translate.instant('COURSE_ANNOUNCEMENTS.SWAL_DELETE_CONFIRM')
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.deleteAnnouncement(this.course.id, announcementId).subscribe({
          next: () => {
            this.toastr.success(this.translate.instant('COURSE_ANNOUNCEMENTS.TOAST_DELETE_SUCCESS'));
            this.announcements.update(list => list.filter(a => a.id !== announcementId));
          },
          error: (err) => {
            this.toastr.error(err.error?.message || this.translate.instant('COURSE_ANNOUNCEMENTS.TOAST_DELETE_ERR'));
          }
        });
      }
    });
  }

  // ── Reordering Methods (HTML5 Drag & Drop) ──────────────────────────────────

  toggleReorderMode(): void {
    if (this.isReordering()) {
      // If exiting and there are unsaved changes, prompt or cancel
      if (this.hasUnsavedOrder()) {
        this.cancelReorder();
      } else {
        this.isReordering.set(false);
      }
    } else {
      // Take snapshot before starting reorder
      this.originalAnnouncementsSnapshot = [...this.announcements()];
      this.isReordering.set(true);
      this.hasUnsavedOrder.set(false);
    }
  }

  onDragStart(event: DragEvent, index: number): void {
    if (!this.isReordering()) return;
    this.draggedIndex.set(index);
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', index.toString());
    }
  }

  onDragOver(event: DragEvent, index: number): void {
    if (!this.isReordering() || this.draggedIndex() === null) return;
    const fromIndex = this.draggedIndex()!;

    // Restrict reordering within the same priority group
    const list = this.announcements();
    if (list[fromIndex].priority !== list[index].priority) {
      return; // Do not allow drag over items of a different priority
    }

    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'move';
    }
    this.dragOverIndex.set(index);
  }

  onDrop(event: DragEvent, targetIndex: number): void {
    if (!this.isReordering() || this.draggedIndex() === null) return;
    event.preventDefault();

    const fromIndex = this.draggedIndex()!;
    this.draggedIndex.set(null);
    this.dragOverIndex.set(null);

    if (fromIndex === targetIndex) return;

    const list = [...this.announcements()];
    // Confirm priority match
    if (list[fromIndex].priority !== list[targetIndex].priority) return;

    // Perform swap/move
    const [movedItem] = list.splice(fromIndex, 1);
    list.splice(targetIndex, 0, movedItem);

    this.announcements.set(list);
    this.hasUnsavedOrder.set(true);
  }

  onDragEnd(): void {
    this.draggedIndex.set(null);
    this.dragOverIndex.set(null);
  }

  saveOrder(): void {
    if (!this.hasUnsavedOrder() || this.isSavingOrder()) return;

    this.isSavingOrder.set(true);

    const list = this.announcements();
    // Build items with displayOrder: Important group starts at 0, Normal group starts at 0
    // Or sequential displayOrder values per priority group
    let importantCounter = 0;
    let normalCounter = 0;

    const items = list.map(ann => {
      const order = ann.priority === 'Important' ? importantCounter++ : normalCounter++;
      return { id: ann.id, displayOrder: order };
    });

    this.announcementService.reorderAnnouncements(this.course.id, { items }).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success(this.translate.instant('COURSE_ANNOUNCEMENTS.TOAST_REORDER_SUCCESS'));
          this.hasUnsavedOrder.set(false);
          this.isReordering.set(false);
          this.originalAnnouncementsSnapshot = [...this.announcements()];
        }
        this.isSavingOrder.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || this.translate.instant('COURSE_ANNOUNCEMENTS.TOAST_REORDER_ERR'));
        this.isSavingOrder.set(false);
      }
    });
  }

  cancelReorder(): void {
    if (this.originalAnnouncementsSnapshot.length > 0) {
      this.announcements.set([...this.originalAnnouncementsSnapshot]);
    }
    this.hasUnsavedOrder.set(false);
    this.isReordering.set(false);
    this.draggedIndex.set(null);
    this.dragOverIndex.set(null);
  }
}

