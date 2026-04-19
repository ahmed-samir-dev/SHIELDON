import { Component, Input, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AnnouncementService, AnnouncementResponse } from '../services/announcement.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-course-announcements',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: './course-announcements.html',
  styleUrls: ['./course-announcements.scss']
})
export class CourseAnnouncementsComponent implements OnInit {
  @Input() course!: CourseDetailResponse;

  private readonly announcementService = inject(AnnouncementService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toastr = inject(ToastrService);

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
        this.toastr.error(err.error?.message || 'Failed to load announcements');
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
          this.toastr.success('Announcement posted successfully!');
          // Important announcements pin to top; Normal ones also at top (API returns sorted)
          this.announcements.update(list => {
            if (res.data!.priority === 'Important') {
              return [res.data!, ...list];
            }
            // Insert after any existing Important announcements
            const firstNormal = list.findIndex(a => a.priority === 'Normal');
            if (firstNormal === -1) return [...list, res.data!];
            return [...list.slice(0, firstNormal), res.data!, ...list.slice(firstNormal)];
          });
          this.togglePostForm();
        }
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to post announcement');
        this.isSubmitting.set(false);
      }
    });
  }

  deleteAnnouncement(announcementId: string): void {
    Swal.fire({
      title: 'Delete Announcement?',
      text: 'This will permanently remove this announcement for all students.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, delete it'
    }).then((result) => {
      if (result.isConfirmed) {
        this.announcementService.deleteAnnouncement(this.course.id, announcementId).subscribe({
          next: () => {
            this.toastr.success('Announcement deleted successfully');
            this.announcements.update(list => list.filter(a => a.id !== announcementId));
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to delete announcement');
          }
        });
      }
    });
  }
}
