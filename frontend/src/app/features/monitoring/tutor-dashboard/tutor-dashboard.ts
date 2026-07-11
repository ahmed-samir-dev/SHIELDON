import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Monitor, Search, Filter, AlertTriangle, ShieldAlert, Users, Clock, ArrowRight, Eye, ChevronLeft, ChevronRight, Activity, FileText, ChevronDown, CheckCircle, Info, BookOpen, FileStack, CheckSquare, SlidersHorizontal, X, Target, Award, TrendingUp } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { MonitoringService, TutorDashboardResponse, SubmissionRow, ExamMonitoringSummary } from '../../../core/services/monitoring.service';
import { LanguageService } from '../../../core/services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { DashboardSignalRService } from '../../../core/services/dashboard-signalr.service';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

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
  private signalR = inject(DashboardSignalRService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;
  private signalRSub!: Subscription;

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
  ChevronDown = ChevronDown;
  CheckCircle = CheckCircle;
  Info = Info;
  BookOpen = BookOpen;
  FileStack = FileStack;
  CheckSquare = CheckSquare;
  SlidersHorizontal = SlidersHorizontal;
  X = X;
  Target = Target;
  Award = Award;
  TrendingUp = TrendingUp;

  Math = Math;

  loading = signal<boolean>(true);
  error = signal<string>('');
  dashboardData = signal<TutorDashboardResponse>({
    examSummaries: [],
    recentSubmissions: [],
    totalSubmissions: 0,
    page: 1,
    pageSize: 10,
    violationTypeDistribution: [],
    totalActiveCourses: 0,
    totalStudents: 0,
    activeExams: 0,
    averagePassRate: 0,
    completionRate: 0,
    totalPassedStudents: 0,
    averageTimeMinutes: 0,
    courseViolationDetails: []
  });

  // Filtering & Pagination for Submissions Table
  searchQuery = signal<string>('');
  statusFilter = signal<string>('All'); // All | Submitted | ForceSubmitted | Graded
  examFilter = signal<string>('');
  submissionsPage = signal<number>(1);
  submissionsPageSize = 10;
  
  // Attempt Grouping
  expandedSubmissions = signal<Set<string>>(new Set());
  
  private filterSubject = new Subject<void>();

  // Course Grouping & Pagination for Exam Cards
  courseSearchQuery = signal<string>('');
  expandedCourses = signal<Set<string>>(new Set());
  coursePage = signal<number>(1);
  coursesPerPage = 4;

  enrollmentsChartOption = signal<EChartsOption>({});

  // Chart Filters
  statusChartCourseFilter = signal<string>('All');
  scoreViolationsCourseFilter = signal<string>('All');
  uniqueCourses = computed(() => Array.from(new Set(this.dashboardData().examSummaries.map(e => e.courseTitle || 'Other'))));

  submissionStatusChartOption = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    let summaries = data.examSummaries || [];
    
    if (this.statusChartCourseFilter() !== 'All') {
      summaries = summaries.filter(s => (s.courseTitle || 'Other') === this.statusChartCourseFilter());
    }

    const examNames = summaries.map(e => e.examTitle);
    const inProgress = summaries.map(e => e.inProgressCount);
    const submitted = summaries.map(e => e.submittedCount);
    const timeout = summaries.map(e => e.timeoutCount || 0);
    const violationLimit = summaries.map(e => e.violationLimitCount || 0);
    const passed = summaries.map(e => e.passedCount || 0);
    const failed = summaries.map(e => e.failedCount || 0);

    return {
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, confine: true },
      legend: { bottom: 0 },
      grid: { left: '8%', right: '4%', bottom: '15%', top: '10%', containLabel: true },
      xAxis: { 
        type: 'category', 
        data: examNames, 
        axisLabel: { 
          interval: 0, 
          rotate: 30,
          formatter: (value: string) => value.length > 12 ? value.substring(0, 10) + '...' : value
        } 
      },
      yAxis: { type: 'value', minInterval: 1 },
      series: [
        { name: 'Live', type: 'bar', stack: 'status', data: inProgress, itemStyle: { color: '#0ea5e9' } },
        { name: 'Submitted', type: 'bar', stack: 'status', data: submitted, itemStyle: { color: '#8b5cf6' } },
        { name: 'Timeout', type: 'bar', stack: 'status', data: timeout, itemStyle: { color: '#f59e0b' } },
        { name: 'Violation Limit', type: 'bar', stack: 'status', data: violationLimit, itemStyle: { color: '#be123c' } },
        
        { name: 'Passed', type: 'bar', stack: 'score', data: passed, itemStyle: { color: '#10b981' } },
        { name: 'Failed', type: 'bar', stack: 'score', data: failed, itemStyle: { color: '#ef4444' } }
      ]
    };
  });

  violationDetailsCourseFilter = signal<string>('All');
  
  violationDetailsChartOption = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    let details = data.courseViolationDetails || [];
    
    if (this.violationDetailsCourseFilter() !== 'All') {
      details = details.filter(d => (d.courseTitle || 'Other') === this.violationDetailsCourseFilter());
    }

    const typeMap = new Map<string, { Minor: number, Medium: number, Critical: number }>();
    details.forEach(d => {
      if (!typeMap.has(d.violationType)) typeMap.set(d.violationType, { Minor: 0, Medium: 0, Critical: 0 });
      const entry = typeMap.get(d.violationType)!;
      if (d.severity === 'Minor') entry.Minor += d.count;
      else if (d.severity === 'Medium') entry.Medium += d.count;
      else if (d.severity === 'Critical') entry.Critical += d.count;
    });

    const types = Array.from(typeMap.keys());
    const minors = types.map(t => typeMap.get(t)!.Minor);
    const mediums = types.map(t => typeMap.get(t)!.Medium);
    const criticals = types.map(t => typeMap.get(t)!.Critical);

    const pieData = types.map(t => {
      const entry = typeMap.get(t)!;
      return { name: t, value: entry.Minor + entry.Medium + entry.Critical };
    });

    return {
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, confine: true },
      legend: [
        { data: ['Minor', 'Medium', 'Critical'], bottom: 25 },
        { data: types, bottom: 0 }
      ],
      grid: { left: '5%', right: '45%', bottom: '25%', top: '10%', containLabel: true },
      xAxis: { 
        type: 'category', 
        data: types, 
        axisLabel: { 
          interval: 0, 
          rotate: 30,
          formatter: (value: string) => value.length > 15 ? value.substring(0, 13) + '...' : value
        } 
      },
      yAxis: { type: 'value', minInterval: 1 },
      series: [
        { name: 'Minor', type: 'bar', stack: 'severity', data: minors, itemStyle: { color: '#10b981' } },
        { name: 'Medium', type: 'bar', stack: 'severity', data: mediums, itemStyle: { color: '#eab308' } },
        { name: 'Critical', type: 'bar', stack: 'severity', data: criticals, itemStyle: { color: '#ef4444' } },
        {
          name: this.translate.instant('TUTOR_DASHBOARD.KPI_VIOLATIONS'),
          type: 'pie',
          radius: ['40%', '70%'],
          center: ['78%', '45%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 10, borderColor: '#fff', borderWidth: 2 },
          label: { 
            show: true, 
            formatter: '{b}\n{d}%',
            position: 'outside'
          },
          labelLine: { show: true },
          data: pieData,
          tooltip: {
            trigger: 'item',
            formatter: '{a} <br/>{b}: {c} ({d}%)'
          }
        }
      ]
    };
  });

  scoreVsViolationsChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    let summaries = data.examSummaries || [];
    
    if (this.scoreViolationsCourseFilter() !== 'All') {
      summaries = summaries.filter(s => (s.courseTitle || 'Other') === this.scoreViolationsCourseFilter());
    }

    if (summaries.length > 0) {
      const examNames = summaries.map(e => e.examTitle.length > 15 ? e.examTitle.substring(0, 15) + '...' : e.examTitle);
      const scores = summaries.map(e => Number((e.averageScore || 0).toFixed(2)));
      const violations = summaries.map(e => e.totalViolations);

      return {
        tooltip: { trigger: 'axis', axisPointer: { type: 'cross' }, confine: true },
        legend: { data: [
          this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_AVG_SCORE'),
          this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_TOTAL_VIOLATIONS')
        ], bottom: 0 },
        grid: { left: '3%', right: '3%', bottom: '15%', top: '15%', containLabel: true },
        xAxis: [{ type: 'category', data: examNames, axisPointer: { type: 'shadow' }, axisLabel: { color: '#64748b', rotate: 30, fontSize: 10 } }],
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
              color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: '#8b5cf6' }, { offset: 1, color: '#c4b5fd' }] },
              borderRadius: [4, 4, 0, 0]
            }
          },
          {
            name: this.translate.instant('TUTOR_DASHBOARD.CHART_LEGEND_TOTAL_VIOLATIONS'), type: 'line', yAxisIndex: 1, data: violations,
            smooth: true, itemStyle: { color: '#f97316' }, lineStyle: { width: 3 }
          }
        ]
      };
    }
    return {};
  });

  // --- Computed Signals for Performance ---

  // Row 1: Scale & Activity
  totalCourses = computed(() => this.dashboardData().totalActiveCourses || 0);
  totalStudents = computed(() => this.dashboardData().totalStudents || 0);
  totalExams = computed(() => this.dashboardData().examSummaries.length);
  activeExams = computed(() => this.dashboardData().activeExams || 0);
  totalSubmissionsKPI = computed(() => this.dashboardData().totalSubmissions);

  // Row 2: Performance, Efficiency & Integrity
  completionRate = computed(() => this.dashboardData().completionRate || 0);
  totalPassedStudents = computed(() => this.dashboardData().totalPassedStudents || 0);
  averagePassRate = computed(() => this.dashboardData().averagePassRate || 0);
  averageTimeMinutes = computed(() => this.dashboardData().averageTimeMinutes || 0);
  totalViolationsKPI = computed(() => this.dashboardData().examSummaries.reduce((acc, curr) => acc + curr.totalViolations, 0));

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

    this.signalR.startConnection();
    this.signalRSub = this.signalR.dashboardUpdated$.subscribe(() => {
      // Quiet reload
      this.loadDashboardSilent();
    });
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
    this.signalRSub?.unsubscribe();
    this.signalR.stopConnection();
  }

  loadDashboardSilent() {
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
          // Don't re-init charts abruptly if not needed, or let computed handle it
        }
      }
    });
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
    const expanded = new Set(this.expandedCourses());
    if (expanded.has(courseTitle)) {
      expanded.delete(courseTitle);
    } else {
      expanded.add(courseTitle);
    }
    this.expandedCourses.set(expanded);
  }

  exportToCsv() {
    const data = this.dashboardData();
    if (!data) return;

    let csvContent = "data:text/csv;charset=utf-8,";
    
    // 1. KPI Overview
    csvContent += "=== KPI OVERVIEW ===\n";
    csvContent += "Metric,Value\n";
    csvContent += `Total Active Courses,${data.totalActiveCourses}\n`;
    csvContent += `Total Students,${data.totalStudents}\n`;
    csvContent += `Active Exams,${data.activeExams}\n`;
    csvContent += `Total Submissions,${data.totalSubmissions}\n`;
    csvContent += `Completion Rate,${data.completionRate}%\n`;
    csvContent += `Average Pass Rate,${data.averagePassRate}%\n`;
    csvContent += `Total Passed Students,${data.totalPassedStudents}\n`;
    csvContent += `Average Time (Mins),${data.averageTimeMinutes}\n\n`;

    // 2. Exam Summaries
    csvContent += "=== EXAM SUMMARIES ===\n";
    csvContent += "Exam Title,Course,Total Enrolled,In Progress,Submitted,Force Submitted,Timeouts,Violation Limit,Not Started,Total Violations,Critical,Avg Score,Passed,Failed\n";
    if (data.examSummaries) {
      data.examSummaries.forEach(e => {
        csvContent += `"${e.examTitle}","${e.courseTitle}",${e.totalEnrolled},${e.inProgressCount},${e.submittedCount},${e.forceSubmittedCount},${e.timeoutCount},${e.violationLimitCount},${e.notStartedCount},${e.totalViolations},${e.criticalViolations},${e.averageScore || 0},${e.passedCount},${e.failedCount}\n`;
      });
    }
    csvContent += "\n";

    // 3. Violation Type Distribution
    csvContent += "=== VIOLATION TYPE DISTRIBUTION ===\n";
    csvContent += "Violation Type,Count\n";
    if (data.violationTypeDistribution) {
      data.violationTypeDistribution.forEach(v => {
        csvContent += `"${v.violationType}",${v.count}\n`;
      });
    }
    csvContent += "\n";

    // 4. Course Violation Details
    csvContent += "=== COURSE VIOLATION DETAILS ===\n";
    csvContent += "Course,Violation Type,Severity,Count\n";
    if (data.courseViolationDetails) {
      data.courseViolationDetails.forEach(v => {
        csvContent += `"${v.courseTitle}","${v.violationType}","${v.severity}",${v.count}\n`;
      });
    }
    csvContent += "\n";

    // 5. Recent Submissions
    csvContent += "=== RECENT SUBMISSIONS ===\n";
    csvContent += "Student Name,Code,Exam Title,Status,Duration (Mins),Score,Passed,Failed,Violations,Highest Severity\n";
    if (data.recentSubmissions) {
      data.recentSubmissions.forEach(s => {
        csvContent += `"${s.studentName}","${s.studentCode}","${s.examTitle}","${s.status}",${s.durationMinutes || 0},${s.score || 0},${s.passed},${s.failed},${s.violationCount},"${s.highestSeverity}"\n`;
      });
    }

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", "Tutor_Dashboard_Report.csv");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  changePage(delta: number) {
    const newPage = this.coursePage() + delta;
    if (newPage >= 1 && newPage <= this.totalPages()) {
      this.coursePage.set(newPage);
    }
  }

  private initCharts(data: TutorDashboardResponse) {
    const isRtl = document.documentElement.dir === 'rtl';

    // 1. Enrollments Chart (Max Enrolled per Course)
    const courseEnrollments = new Map<string, number>();
    data.examSummaries.forEach(ex => {
      const currentMax = courseEnrollments.get(ex.courseTitle) || 0;
      if (ex.totalEnrolled > currentMax) {
        courseEnrollments.set(ex.courseTitle, ex.totalEnrolled);
      }
    });
    
    const courseNames = Array.from(courseEnrollments.keys());
    const enrolledData = Array.from(courseEnrollments.values());

    this.enrollmentsChartOption.set({
      tooltip: { trigger: 'axis', confine: true },
      grid: { left: '3%', right: '4%', bottom: '10%', top: '10%', containLabel: true },
      xAxis: { type: 'value', minInterval: 1 },
      yAxis: { type: 'category', data: courseNames, axisLabel: { interval: 0 } },
      series: [{
        name: this.translate.instant('TUTOR_DASHBOARD.KPI_ENROLLMENTS'),
        type: 'bar',
        data: enrolledData,
        itemStyle: { color: '#6366f1', borderRadius: [0, 4, 4, 0] }
      }]
    });
  }

  trackByExamId(index: number, item: ExamMonitoringSummary) { return item.examId; }
  trackBySessionId(index: number, item: SubmissionRow) { return item.attemptId; }
  trackByGroupKey(index: number, item: any) { return item.key; }
  trackByCourseGroup(index: number, item: { courseTitle: string, exams: ExamMonitoringSummary[] }) { return item.courseTitle; }

  getStatusDisplay(status: string) {
    switch (status) {
      case 'InProgress': return { label: this.translate.instant('TUTOR_DASHBOARD.FILTER_ACTIVE'), icon: this.Info, classes: 'bg-blue-100 text-blue-700' };
      case 'Submitted': return { label: this.translate.instant('TUTOR_DASHBOARD.STATUS_SUBMITTED'), icon: this.CheckCircle, classes: 'bg-green-100 text-green-700' };
      case 'Graded': return { label: this.translate.instant('TUTOR_DASHBOARD.STATUS_GRADED'), icon: this.CheckCircle, classes: 'bg-emerald-100 text-emerald-700' };
      case 'ForceSubmitted': return { label: this.translate.instant('TUTOR_DASHBOARD.STATUS_TERMINATED'), icon: this.ShieldAlert, classes: 'bg-red-100 text-red-700' };
      default: return { label: this.translate.instant('EXAM_ENGINE.STATUS_' + status.toUpperCase()), icon: this.Info, classes: 'bg-slate-100 text-slate-700' };
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

  toggleSubmissionDetails(key: string) {
    const current = new Set(this.expandedSubmissions());
    if (current.has(key)) {
      current.delete(key);
    } else {
      current.add(key);
    }
    this.expandedSubmissions.set(current);
  }
}
