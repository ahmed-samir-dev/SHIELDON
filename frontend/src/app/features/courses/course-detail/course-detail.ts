import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CourseService } from '../services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import { ToastrService } from 'ngx-toastr';
import { CourseMaterialsComponent } from '../course-materials/course-materials';
import { CourseAnnouncementsComponent } from '../course-announcements/course-announcements';
import { CourseAssignmentsComponent } from '../course-assignments/course-assignments';
import { CourseExamsComponent } from '../course-exams/course-exams';
import { CourseQuestionBankComponent } from '../question-bank/course-question-bank.component';
import { CourseGrades } from '../../grades/course-grades/course-grades';
import { CourseLeaderboardComponent } from '../course-leaderboard/course-leaderboard';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

type CourseTab = 'announcements' | 'materials' | 'assignments' | 'exams' | 'question-bank' | 'grades' | 'leaderboard';
const VALID_TABS: CourseTab[] = ['announcements', 'materials', 'assignments', 'exams', 'question-bank', 'grades', 'leaderboard'];

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    CourseMaterialsComponent,
    CourseAnnouncementsComponent,
    CourseAssignmentsComponent,
    CourseExamsComponent,
    CourseQuestionBankComponent,
    CourseGrades,
    CourseLeaderboardComponent,
    TranslateModule,
  ],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss'
})
export class CourseDetail implements OnInit {
  private translate = inject(TranslateService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private courseService = inject(CourseService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);

  course = signal<CourseDetailResponse | null>(null);
  isLoading = signal(true);
  activeTab = signal<CourseTab>('announcements');

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.loadCourse(id);
      }
    });

    // Listen for query parameter changes to activate the correct tab
    this.route.queryParamMap.subscribe(queryParams => {
      const tab = queryParams.get('tab');
      if (tab && (VALID_TABS as string[]).includes(tab)) {
        this.activeTab.set(tab as CourseTab);
      }
    });
  }

  loadCourse(id: string) {
    this.isLoading.set(true);
    this.courseService.getCourse(id).subscribe({
      next: (res) => {
        if (this.authService.isStudent()) {
          this.courseService.getMyEnrollments({ pageSize: 1000 }).subscribe({
            next: (enrollRes) => {
              const myEnrollment = enrollRes.data.items.find(e => e.courseId === id);
              if (!myEnrollment || myEnrollment.status !== 'Approved') {
                this.toastr.warning('Access Restricted: You must have an approved enrollment to view course details.', 'Access Restricted');
                this.router.navigateByUrl('/courses');
                return;
              }
              this.course.set(res.data);
              this.isLoading.set(false);
            },
            error: () => {
              this.toastr.error(this.translate.instant('COURSE_DETAIL.TOAST_LOAD_ERR'));
              this.router.navigateByUrl('/courses');
              this.isLoading.set(false);
            }
          });
        } else {
          this.course.set(res.data);
          this.isLoading.set(false);
        }
      },
      error: () => {
        this.toastr.error(this.translate.instant('COURSE_DETAIL.TOAST_LOAD_ERR'));
        this.isLoading.set(false);
      }
    });
  }

  setActiveTab(tab: CourseTab) {
    this.activeTab.set(tab);
    // Sync the active tab backward to the URL to allow sharable URLs and persistent state
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab },
      queryParamsHandling: 'merge'
    });
  }
}
