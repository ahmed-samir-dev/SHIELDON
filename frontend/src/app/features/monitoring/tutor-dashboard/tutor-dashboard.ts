import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Monitor, Search, Filter, AlertTriangle, ShieldAlert, Users, Clock, ArrowRight, Eye, ChevronLeft, ChevronRight, Activity, FileText } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { MonitoringService, TutorDashboardResponse, SubmissionRow, ExamMonitoringSummary } from '../../../core/services/monitoring.service';
import { LanguageService } from '../../../core/services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-tutor-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, LucideAngularModule, NgxEchartsModule, TranslateModule],
  templateUrl: './tutor-dashboard.html',
  styleUrls: ['./tutor-dashboard.scss']
})
export class TutorDashboardComponent implements OnInit, OnDestroy {
  private monitoring = inject(MonitoringService);
  private router = inject(Router);
  private languageService = inject(LanguageService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;

  // Icons
  Monitor = Monitor;
  Search = Search;
  Filter = Filter;
  AlertTriangle = AlertTriangle;
  ShieldAlert = ShieldAlert;
  Users = Users;
  Clock = Clock;
  ArrowRight = ArrowRight;
  Eye = Eye;
  ChevronLeft = ChevronLeft;
  ChevronRight = ChevronRight;
  Activity = Activity;
  FileText = FileText;

  Math = Math;

  loading = signal<boolean>(true);
  error = signal<string>('');
  dashboardData = signal<TutorDashboardResponse>({
    examSummaries: [],
    recentSubmissions: [],
    totalSubmissions: 0,
    page: 1,
    pageSize: 10,
    violationTypeDistribution: []
  });

  // Filtering & Pagination for Submissions Table
  searchQuery = signal<string>('');
  statusFilter = signal<string>('All'); // All | Submitted | ForceSubmitted | Graded
  examFilter = signal<string>('');
  submissionsPage = signal<number>(1);
  submissionsPageSize = 10;
  
  private filterSubject = new Subject<void>();

  // Course Grouping & Pagination for Exam Cards
  courseSearchQuery = signal<string>('');
  expandedCourses = signal<Set<string>>(new Set());
  coursePage = signal<number>(1);
  coursesPerPage = 4;

  // Charts
  violationChartOptions = signal<EChartsOption>({});
  submissionStatusChartOptions = signal<EChartsOption>({});
  scoreVsViolationsChartOptions = signal<EChartsOption>({});
  private lastViolationChartData = '';
  private lastExamSummaryData = '';

  // --- Computed Signals for Performance ---

  groupedCourses = computed(() => {
    const exams = this.dashboardData().examSummaries;
    const groups = new Map<string, { courseTitle: string, exams: ExamMonitoringSummary[] }>();
    
    exams.forEach(exam => {
      const courseTitle = exam.courseTitle || 'Other';
      if (!groups.has(courseTitle)) {
        groups.set(courseTitle, { courseTitle, exams: [] });
      }
      groups.get(courseTitle)!.exams.push(exam);
    });

    return Array.from(groups.values());
  });

  filteredCourses = computed(() => {
    const groups = this.groupedCourses();
    const query = this.courseSearchQuery().toLowerCase();
    
    if (!query) return groups;

    return groups.filter((g: { courseTitle: string, exams: ExamMonitoringSummary[] }) => 
      g.courseTitle.toLowerCase().includes(query) || 
      g.exams.some((e: ExamMonitoringSummary) => e.examTitle.toLowerCase().includes(query))
    );
  });

  totalPages = computed(() => {
    return Math.ceil(this.filteredCourses().length / this.coursesPerPage);
  });

  paginatedCourses = computed(() => {
    const courses = this.filteredCourses();
    const startIndex = (this.coursePage() - 1) * this.coursesPerPage;
    return courses.slice(startIndex, startIndex + this.coursesPerPage);
  });

  // -----------------------------------------

  ngOnInit() {
    this.filterSubject.pipe(debounceTime(300)).subscribe(() => {
      this.submissionsPage.set(1);
      this.loadDashboard();
    });

    this.loadDashboard();
    this.langSub = this.languageService.languageChange$.subscribe(() => this.loadDashboard());
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
  }

  loadDashboard() {
    this.loading.set(true);
    const examId = this.examFilter() || undefined;

    this.monitoring.getTutorDashboard(
      this.submissionsPage(),
      this.submissionsPageSize,
      this.searchQuery(),
      this.statusFilter(),
      examId
    ).subscribe({
      next: (res) => {
        if (res.data) {
          this.dashboardData.set(res.data);
          this.initCharts(res.data);
        }
        this.loading.set(false);
      },
      error: () => {
        if (!this.dashboardData().examSummaries.length) {
          this.error.set(this.translate.instant('TUTOR_DASHBOARD.ERR_LOAD'));
        }
        this.loading.set(false);
      }
    });
  }

  onSearchInput(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
    this.filterSubject.next();
  }

  onStatusChange(event: Event) {
    this.statusFilter.set((event.target as HTMLSelectElement).value);
    this.filterSubject.next();
  }

  onExamFilterChange(event: Event) {
    this.examFilter.set((event.target as HTMLSelectElement).value);
    this.filterSubject.next();
  }

  changeLivePage(delta: number) {
    const newPage = this.submissionsPage() + delta;
    const maxPage = Math.ceil(this.dashboardData().totalSubmissions / this.submissionsPageSize);
    if (newPage >= 1 && newPage <= maxPage) {
      this.submissionsPage.set(newPage);
      this.loadDashboard();
    }
  }

  toggleCourse(courseTitle: string) {
    const current = new Set(this.expandedCourses());
    if (current.has(courseTitle)) {
      current.delete(courseTitle);
    } else {
      current.add(courseTitle);
    }
    this.expandedCourses.set(current);
  }

  changePage(delta: number) {
    const newPage = this.coursePage() + delta;
    if (newPage >= 1 && newPage <= this.totalPages()) {
      this.coursePage.set(newPage);
    }
  }

  private initCharts(data: TutorDashboardResponse) {
    if (data.violationTypeDistribution && data.violationTypeDistribution.length > 0) {
      const chartData = data.violationTypeDistribution.map(item => ({
        name: item.violationType,
        value: item.count
      }));

      const chartDataStr = JSON.stringify(chartData);
      if (this.lastViolationChartData === chartDataStr) {
        // Skip only the violation chart - don't return from the entire method
      } else {
        this.lastViolationChartData = chartDataStr;

      this.violationChartOptions.set({
        tooltip: { trigger: 'item', appendToBody: true },
        legend: { 
          orient: 'horizontal',
          bottom: '0',
          left: 'center',
          icon: 'circle', 
          textStyle: { fontSize: 12, color: '#475569' },
          itemWidth: 12,
          itemHeight: 12
        },
        series: [
          {
            name: this.translate.instant('TUTOR_DASHBOARD.CHART_SERIES_VIOLATIONS'),
            type: 'pie',
            radius: ['45%', '70%'],
            center: ['50%', '45%'],
            avoidLabelOverlap: true,
            itemStyle: {
              borderRadius: 8,
              borderColor: '#fff',
              borderWidth: 2
            },
            label: { 
              show: true,
              position: 'outside',
              formatter: '{b}: {c} ({d}%)',
              fontSize: 12,
              color: '#334155'
            },
            emphasis: {
              label: { show: true, fontSize: 14, fontWeight: 'bold' }
            },
            labelLine: { 
              show: true,
              length: 15,
              length2: 10
            },
            data: chartData
          }
        ]
      });
      } // end else (dedup check)
    }

    // New Charts based on ExamSummaries
    if (data.examSummaries && data.examSummaries.length > 0) {
      const summaryDataStr = JSON.stringify(data.examSummaries);
      if (this.lastExamSummaryData !== summaryDataStr) {
        this.lastExamSummaryData = summaryDataStr;

        const examNames = data.examSummaries.map(e => e.examTitle.length > 15 ? e.examTitle.substring(0, 15) + '...' : e.examTitle);
        const submitted = data.examSummaries.map(e => e.submittedCount);
        const inProgress = data.examSummaries.map(e => e.inProgressCount);
        const terminated = data.examSummaries.map(e => e.forceSubmittedCount);
        const scores = data.examSummaries.map(e => e.averageScore || 0);
        const violations = data.examSummaries.map(e => e.totalViolations);

        // 1. Stacked Bar Chart: Submission Status Breakdown
        this.submissionStatusChartOptions.set({
          tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
          legend: { data: [
            this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_DONE'),
            this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_ACTIVE'),
            this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_TERMINATED')
          ], bottom: 0 },
          grid: { left: '3%', right: '4%', bottom: '15%', top: '10%', containLabel: true },
          xAxis: { type: 'category', data: examNames, axisLabel: { color: '#64748b', rotate: 30 } },
          yAxis: { type: 'value', axisLine: { show: false }, splitLine: { lineStyle: { color: '#f1f5f9', type: 'dashed' } } },
          series: [
            { name: this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_DONE'), type: 'bar', stack: 'total', data: submitted, itemStyle: { color: '#10b981' } }, // emerald-500
            { name: this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_ACTIVE'), type: 'bar', stack: 'total', data: inProgress, itemStyle: { color: '#3b82f6' } }, // blue-500
            { name: this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_TERMINATED'), type: 'bar', stack: 'total', data: terminated, itemStyle: { borderRadius: [4, 4, 0, 0], color: '#ef4444' } } // red-500
          ]
        });

        // 2. Dual Axis Chart: Score vs Violations
        this.scoreVsViolationsChartOptions.set({
          tooltip: { trigger: 'axis', axisPointer: { type: 'cross' } },
          legend: { data: [
            this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_AVG_SCORE'),
            this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_TOTAL_VIOLATIONS')
          ], bottom: 0 },
          grid: { left: '3%', right: '3%', bottom: '15%', top: '15%', containLabel: true },
          xAxis: [{ type: 'category', data: examNames, axisPointer: { type: 'shadow' }, axisLabel: { color: '#64748b' } }],
          yAxis: [
            {
              type: 'value', name: 'Score', min: 0, max: 100,
              axisLabel: { formatter: '{value} %', color: '#64748b' },
              splitLine: { show: false }
            },
            {
              type: 'value', name: 'Violations', minInterval: 1,
              axisLabel: { formatter: '{value}', color: '#64748b' },
              splitLine: { lineStyle: { color: '#f1f5f9', type: 'dashed' } }
            }
          ],
          series: [
            {
              name: this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_AVG_SCORE'), type: 'bar', data: scores,
              itemStyle: { 
                color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: '#8b5cf6' }, { offset: 1, color: '#c4b5fd' }] }, // violet gradient
                borderRadius: [4, 4, 0, 0]
              }
            },
            {
              name: this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_TOTAL_VIOLATIONS'), type: 'line', yAxisIndex: 1, data: violations,
              smooth: true, itemStyle: { color: '#f97316' }, lineStyle: { width: 3 } // orange-500
            }
          ]
        });
      }
    }
  }

  trackByExamId(index: number, item: ExamMonitoringSummary) { return item.examId; }
  trackBySessionId(index: number, item: SubmissionRow) { return item.attemptId; }
  trackByCourseGroup(index: number, item: { courseTitle: string, exams: ExamMonitoringSummary[] }) { return item.courseTitle; }

  getStatusDisplay(status: string) {
    switch (status) {
      case 'Submitted': return { label: this.translate.instant('TUTOR_DASHBOARD.STATUS_SUBMITTED'), icon: 'check-circle', classes: 'bg-green-100 text-green-700' };
      case 'Graded': return { label: this.translate.instant('TUTOR_DASHBOARD.STATUS_GRADED'), icon: 'check-circle', classes: 'bg-emerald-100 text-emerald-700' };
      case 'ForceSubmitted': return { label: this.translate.instant('TUTOR_DASHBOARD.STATUS_TERMINATED'), icon: 'shield-alert', classes: 'bg-red-100 text-red-700' };
      default: return { label: this.translate.instant('EXAM_ENGINE.STATUS_' + status.toUpperCase()), icon: 'info', classes: 'bg-slate-100 text-slate-700' };
    }
  }

  getSeverityBadge(severity: string) {
    switch (severity) {
      case 'Critical': return 'bg-red-100 text-red-700 border-red-200';
      case 'Medium': return 'bg-amber-100 text-amber-700 border-amber-200';
      case 'Minor': return 'bg-blue-100 text-blue-700 border-blue-200';
      default: return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }

  goToTimeline(attemptId: string) {
    this.router.navigate(['/monitoring/attempts', attemptId]);
  }
}
