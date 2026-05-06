import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, BarChart2, CheckCircle, AlertCircle, Award, ChevronDown, ChevronRight } from 'lucide-angular';
import { GradeService, MyGradeItemResponse } from '../services/grade.service';

interface CourseGradesGroup {
  courseId: string;
  courseTitle: string;
  grades: MyGradeItemResponse[];
  totalScore: number;
}

@Component({
  selector: 'app-my-grades',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule],
  templateUrl: './my-grades.html',
  styleUrl: './my-grades.scss'
})
export class MyGrades implements OnInit {
  private gradeService = inject(GradeService);
  private toastr = inject(ToastrService);

  // Icons
  readonly BarChart2 = BarChart2;
  readonly CheckCircle = CheckCircle;
  readonly AlertCircle = AlertCircle;
  readonly Award = Award;
  readonly ChevronDown = ChevronDown;
  readonly ChevronRight = ChevronRight;

  isLoading = signal(true);
  courseGroups = signal<CourseGradesGroup[]>([]);
  expandedCourseIds = signal<Set<string>>(new Set());

  toggleExpand(courseId: string) {
    const expanded = this.expandedCourseIds();
    if (expanded.has(courseId)) {
      expanded.delete(courseId);
    } else {
      expanded.add(courseId);
    }
    this.expandedCourseIds.set(new Set(expanded));
  }

  isExpanded(courseId: string): boolean {
    return this.expandedCourseIds().has(courseId);
  }

  ngOnInit() {
    this.loadMyGrades();
  }

  loadMyGrades() {
    this.isLoading.set(true);
    this.gradeService.getMyGrades().subscribe({
      next: (res) => {
        const groups = this.groupGradesByCourse(res.data);
        this.courseGroups.set(groups);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load your grades');
        this.isLoading.set(false);
      }
    });
  }

  private groupGradesByCourse(grades: MyGradeItemResponse[]): CourseGradesGroup[] {
    const map = new Map<string, CourseGradesGroup>();
    
    for (const g of grades) {
      if (!map.has(g.courseId)) {
        map.set(g.courseId, {
          courseId: g.courseId,
          courseTitle: g.courseTitle,
          grades: [],
          totalScore: 0
        });
      }
      
      const group = map.get(g.courseId)!;
      group.grades.push(g);
      group.totalScore += g.weightedScore;
    }

    return Array.from(map.values());
  }
}
