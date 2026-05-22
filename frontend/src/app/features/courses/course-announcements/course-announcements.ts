import { Component, Input, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
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
  private langSub!: Subscription;

  announcements = signal<AnnouncementResponse[]>([]);
  isLoading = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);
  showPostForm = signal<boolean>(false);

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
    this.showPostForm.update((v) => !v);
    if (!this.showPostForm()) {
      this.postForm.reset({ priority: 'Normal' });
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
}
