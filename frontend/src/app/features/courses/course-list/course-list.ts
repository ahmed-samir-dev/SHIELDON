import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CourseService } from '../services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { CourseResponse, CourseQueryParams, UserBasicResponse, StudentEnrollmentStatusResponse } from '../../../core/models/courses.model';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './course-list.html',
  styleUrl: './course-list.scss'
})
export class CourseList implements OnInit, OnDestroy {
  private translate = inject(TranslateService);
  private courseService = inject(CourseService);
  public authService = inject(AuthService);
  private languageService = inject(LanguageService);
  private toastr = inject(ToastrService);
  private fb = inject(FormBuilder);
  private langSub!: Subscription;

  courses = signal<CourseResponse[]>([]);
  tutors = signal<UserBasicResponse[]>([]);
  myEnrollments = signal<StudentEnrollmentStatusResponse[]>([]);
  isLoading = signal(true);
  
  query: CourseQueryParams = {
    page: 1,
    pageSize: 6,
    search: '',
  };

  // ── Pagination State ─────────────────────────────────────────────────────
  totalPages = signal(1);
  currentPage = signal(1);

  // ── Modal State ──────────────────────────────────────────────────────────
  isModalOpen = signal(false);
  modalMode = signal<'create' | 'edit'>('create');
  selectedCourseId = signal<string | null>(null);
  isSubmitting = signal(false);

  courseForm = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3)]],
    courseCode: ['', [Validators.required, Validators.maxLength(20)]],
    description: [''],
    assignedTutorId: [''],
    courseFee: [0, [Validators.min(0)]]
  });

  ngOnInit() {
    if (this.authService.isStudent()) {
      this.query.enrollmentStatus = 'enrolled';
    }

    this.loadCourses();
    if (this.authService.isAdmin()) {
      this.loadTutors();
    }
    if (this.authService.isStudent()) {
      this.loadMyEnrollments();
    }

    // Auto-reload translated course data whenever the user toggles language
    this.langSub = this.languageService.languageChange$.subscribe(() => {
      this.loadCourses();
    });
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
  }

  loadMyEnrollments() {
    this.courseService.getMyEnrollments({ pageSize: 1000 }).subscribe({
      next: (res) => this.myEnrollments.set(res.data.items),
      error: () => console.error('Failed to load my enrollments.')
    });
  }

  loadTutors() {
    this.courseService.getTutors().subscribe({
      next: (res) => this.tutors.set(res.data),
      error: () => this.toastr.error(this.translate.instant('COURSE_LIST.TOAST_LOAD_TUTORS_ERR'))
    });
  }

  loadCourses() {
    this.isLoading.set(true);
    this.courseService.getCourses(this.query).subscribe({
      next: (res) => {
        this.courses.set(res.data.items);
        this.totalPages.set(Math.ceil(res.data.totalCount / res.data.pageSize));
        this.currentPage.set(res.data.pageNumber);
        this.isLoading.set(false);
      },
      error: () => {
        this.toastr.error(this.translate.instant('COURSE_LIST.TOAST_LOAD_COURSES_ERR'));
        this.isLoading.set(false);
      }
    });
  }

  onSearchChange() {
    this.query.page = 1;
    this.loadCourses();
  }

  setStudentFilter(filter: 'all' | 'enrolled' | 'pending' | 'unenrolled') {
    if (filter === 'all') {
      this.query.enrollmentStatus = null;
    } else {
      this.query.enrollmentStatus = filter;
    }
    this.onSearchChange();
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.query.page = this.currentPage() + 1;
      this.loadCourses();
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.query.page = this.currentPage() - 1;
      this.loadCourses();
    }
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
    this.modalMode.set('edit');
    this.selectedCourseId.set(course.id);
    this.courseForm.patchValue({
      title: course.title,
      courseCode: course.courseCode,
      description: course.description || '',
      assignedTutorId: course.assignedTutorId || '',
      courseFee: course.courseFee || 0
    });
    
    // Disable course code editing for existing courses
    this.courseForm.get('courseCode')?.disable();

    if (!this.authService.isAdmin()) {
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
      assignedTutorId: formVals.assignedTutorId || null,
      courseFee: formVals.courseFee || 0
    };

    if (this.modalMode() === 'create') {
      const createReq = { ...request, courseCode: formVals.courseCode! };
      this.courseService.createCourse(createReq).subscribe({
        next: () => {
          this.toastr.success(this.translate.instant('COURSE_LIST.TOAST_CREATE_SUCCESS'));
          this.closeModal();
          this.loadCourses();
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.toastr.error(err.error?.message || this.translate.instant('COURSE_LIST.TOAST_CREATE_ERR'));
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
          this.toastr.success(this.translate.instant('COURSE_LIST.TOAST_UPDATE_SUCCESS'));
          this.closeModal();
          this.loadCourses();
          this.isSubmitting.set(false);
        },
        error: (err) => {
          this.toastr.error(err.error?.message || this.translate.instant('COURSE_LIST.TOAST_UPDATE_ERR'));
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
      courseFee: course.courseFee,
      isActive: isActive
    };

    this.courseService.updateCourse(course.id, request).subscribe({
      next: () => {
        const status = isActive ? this.translate.instant('COURSE_LIST.TITLE_PUBLISH') : this.translate.instant('COURSE_LIST.TITLE_ARCHIVE');
        this.toastr.success(this.translate.instant('COURSE_LIST.TOAST_STATUS_SUCCESS', { status: status }));
        this.loadCourses();
      },
      error: (err) => {
        const msg = err.error?.message || this.translate.instant('COURSE_LIST.TOAST_STATUS_ERR');
        this.toastr.error(msg);
      }
    });
  }

  requestEnrollment(courseId: string) {
    this.courseService.requestEnrollment(courseId).subscribe({
      next: () => {
        this.toastr.success(this.translate.instant('COURSE_LIST.TOAST_ENROLL_SUCCESS'));
        
        // Real-time optimistic update
        const course = this.courses().find(c => c.id === courseId);
        if (course) {
          const newEnrollment: StudentEnrollmentStatusResponse = {
            courseId: course.id,
            courseTitle: course.title,
            status: 'Pending',
            rejectionCount: 0,
            cooldownUntil: null,
            rejectionReason: null,
            requestedAt: new Date().toISOString()
          };
          
          this.myEnrollments.update(enrollments => {
            const exists = enrollments.find(e => e.courseId === courseId);
            if (exists) {
              return enrollments.map(e => e.courseId === courseId ? { ...e, status: 'Pending' } : e);
            }
            return [...enrollments, newEnrollment];
          });
          
          // Remove from view if filtering by 'Available to enroll'
          if (this.query.enrollmentStatus === 'unenrolled') {
            this.courses.update(courses => courses.filter(c => c.id !== courseId));
          }
        }
      },
      error: (err) => {
        const msg = err.error?.message || this.translate.instant('COURSE_LIST.SWAL_ENROLL_ERR_DESC');
        Swal.fire({
          title: this.translate.instant('COURSE_LIST.SWAL_ENROLL_ERR_TITLE'),
          text: msg,
          icon: 'error',
          confirmButtonColor: '#3b82f6'
        });
      }
    });
  }
}
