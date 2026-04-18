import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CourseService } from '../services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { CourseResponse, CourseQueryParams, UserBasicResponse, StudentEnrollmentStatusResponse } from '../../../core/models/courses.model';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule],
  templateUrl: './course-list.html',
  styleUrl: './course-list.scss'
})
export class CourseList implements OnInit {
  private courseService = inject(CourseService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);

  courses = signal<CourseResponse[]>([]);
  tutors = signal<UserBasicResponse[]>([]);
  myEnrollments = signal<StudentEnrollmentStatusResponse[]>([]);
  isLoading = signal(true);
  
  query: CourseQueryParams = {
    page: 1,
    pageSize: 12,
    search: '',
  };

  // ── Modal State ──────────────────────────────────────────────────────────
  isModalOpen = signal(false);
  modalMode = signal<'create' | 'edit'>('create');
  selectedCourseId = signal<string | null>(null);
  isSubmitting = signal(false);

  courseForm = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3)]],
    courseCode: ['', [Validators.required, Validators.maxLength(20)]],
    description: [''],
    assignedTutorId: ['']
  });

  ngOnInit() {
    this.loadCourses();
    if (this.authService.isAdmin() || this.authService.isTutor()) {
      this.loadTutors();
    }
    if (this.authService.isStudent()) {
      this.loadMyEnrollments();
    }
  }

  loadMyEnrollments() {
    this.courseService.getMyEnrollments().subscribe({
      next: (res) => this.myEnrollments.set(res.data),
      error: () => console.error('Failed to load my enrollments.')
    });
  }

  loadTutors() {
    this.courseService.getTutors().subscribe({
      next: (res) => this.tutors.set(res.data),
      error: () => this.toastr.error('Failed to load tutors.')
    });
  }

  loadCourses() {
    this.isLoading.set(true);
    this.courseService.getCourses(this.query).subscribe({
      next: (res) => {
        this.courses.set(res.data.items);
        this.isLoading.set(false);
      },
      error: () => {
        this.toastr.error('Failed to load courses.');
        this.isLoading.set(false);
      }
    });
  }

  onSearchChange() {
    this.query.page = 1;
    this.loadCourses();
  }

  canManageCourse(course: CourseResponse): boolean {
    if (this.authService.isAdmin()) return true;
    if (this.authService.isTutor() && course.assignedTutorId === this.authService.currentUser()?.userId) return true;
    return false;
  }

  getEnrollmentStatus(courseId: string): string | null {
    const enrollments = this.myEnrollments();
    const existing = enrollments.find(e => e.courseId === courseId);
    return existing ? existing.status : null;
  }

  // ── Modal Actions ────────────────────────────────────────────────────────

  openCreateModal() {
    this.courseForm.reset();
    this.courseForm.get('courseCode')?.enable(); // Code can be set during creation
    this.modalMode.set('create');
    this.selectedCourseId.set(null);
    this.isModalOpen.set(true);
  }

  openEditModal(course: CourseResponse) {
    this.courseForm.patchValue({
      title: course.title,
      courseCode: course.courseCode,
      description: course.description || '',
      assignedTutorId: course.assignedTutorId || ''
    });
    
    // Disable course code editing for existing courses
    this.courseForm.get('courseCode')?.disable();

    // If Tutor, prevent changing the assigned tutor
    if (this.authService.isTutor()) {
      this.courseForm.get('assignedTutorId')?.disable();
    } else {
      this.courseForm.get('assignedTutorId')?.enable();
    }

    this.modalMode.set('edit');
    this.selectedCourseId.set(course.id);
    this.isModalOpen.set(true);
  }

  closeModal() {
    this.isModalOpen.set(false);
  }

  submitCourseForm() {
    if (this.courseForm.invalid) {
      this.courseForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    const formVals = this.courseForm.getRawValue(); // getRawValue to include disabled fields if needed
    
    const request = {
      title: formVals.title!,
      description: formVals.description || null,
      assignedTutorId: formVals.assignedTutorId || null
    };

    if (this.modalMode() === 'create') {
      const createReq = { ...request, courseCode: formVals.courseCode! };
      this.courseService.createCourse(createReq).subscribe({
        next: () => {
          this.toastr.success('Course created successfully!');
          this.closeModal();
          this.loadCourses();
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Failed to create course');
          this.isSubmitting.set(false);
        }
      });
    } else {
      // For Edit, we also need to pass the current IsActive state from the original course
      const currentCourse = this.courses().find(c => c.id === this.selectedCourseId());
      const updateReq = { 
        ...request, 
        isActive: currentCourse ? currentCourse.isActive : true 
      };

      this.courseService.updateCourse(this.selectedCourseId()!, updateReq).subscribe({
        next: () => {
          this.toastr.success('Course updated successfully!');
          this.closeModal();
          this.loadCourses();
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Failed to update course');
          this.isSubmitting.set(false);
        }
      });
    }
  }

  // ── Other Actions ────────────────────────────────────────────────────────

  async setCourseStatus(course: CourseResponse, isActive: boolean) {
    if (!this.canManageCourse(course)) return;

    const request = {
      title: course.title,
      description: course.description,
      assignedTutorId: course.assignedTutorId,
      isActive: isActive
    };

    this.courseService.updateCourse(course.id, request).subscribe({
      next: () => {
        this.toastr.success(`Course ${isActive ? 'published' : 'archived'} successfully.`);
        this.loadCourses();
      },
      error: (err) => {
        const msg = err.error?.message || 'Failed to update course status.';
        this.toastr.error(msg);
      }
    });
  }

  requestEnrollment(courseId: string) {
    this.courseService.requestEnrollment(courseId).subscribe({
      next: () => {
        this.toastr.success('Enrollment request submitted successfully!');
      },
      error: (err) => {
        const msg = err.error?.message || 'Failed to request enrollment. You may be blocked or on cooldown.';
        Swal.fire({
          title: 'Enrollment Error',
          text: msg,
          icon: 'error',
          confirmButtonColor: '#3b82f6'
        });
      }
    });
  }
}
