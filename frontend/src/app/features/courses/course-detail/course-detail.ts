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

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, CourseMaterialsComponent, CourseAnnouncementsComponent, CourseAssignmentsComponent, CourseExamsComponent],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss'
})
// Main course detail component
export class CourseDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private courseService = inject(CourseService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);

  course = signal<CourseDetailResponse | null>(null);
  isLoading = signal(true);
  activeTab = signal<'announcements' | 'materials' | 'assignments' | 'exams'>('announcements');

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
      if (tab && ['announcements', 'materials', 'assignments', 'exams'].includes(tab)) {
        this.activeTab.set(tab as any);
      }
    });
  }

  loadCourse(id: string) {
    this.isLoading.set(true);
    this.courseService.getCourse(id).subscribe({
      next: (res) => {
        this.course.set(res.data);
        this.isLoading.set(false);
      },
      error: () => {
        this.toastr.error('Failed to load course details.');
        this.isLoading.set(false);
      }
    });
  }

  setActiveTab(tab: 'announcements' | 'materials' | 'assignments' | 'exams') {
    this.activeTab.set(tab);
    // Sync the active tab backward to the URL to allow sharable URLs and persistent state
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab },
      queryParamsHandling: 'merge'
    });
  }
}
