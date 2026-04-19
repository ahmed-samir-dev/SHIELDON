import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CourseService } from '../services/course.service';
import { AuthService } from '../../../core/services/auth.service';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import { ToastrService } from 'ngx-toastr';
import { CourseMaterialsComponent } from '../course-materials/course-materials';
import { CourseAnnouncementsComponent } from '../course-announcements/course-announcements';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, CourseMaterialsComponent, CourseAnnouncementsComponent],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss'
})
export class CourseDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private courseService = inject(CourseService);
  public authService = inject(AuthService);
  private toastr = inject(ToastrService);

  course = signal<CourseDetailResponse | null>(null);
  isLoading = signal(true);

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.loadCourse(id);
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
}
