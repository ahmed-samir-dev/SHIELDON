import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Activity, Users, ShieldAlert, FileText, Monitor, CheckCircle, TrendingUp, AlertTriangle, Search, DollarSign, BookOpen, FileStack, FileEdit, CheckSquare, GraduationCap, UserCheck, Award, Target } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, AdminDashboardResponse, ExamStatisticsRow, DailyActivityPoint, PaymentTrendPoint } from '../../../core/services/monitoring.service';
import { ThemeService } from '../../../core/services/theme.service';
import { LanguageService } from '../../../core/services/language.service';
import { UserService } from '../../../core/services/user.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { DashboardSignalRService } from '../../../core/services/dashboard-signalr.service';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, NgxEchartsModule, TranslateModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.scss']
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  private monitoring = inject(MonitoringService);
  private router = inject(Router);
  private themeService = inject(ThemeService);
  private languageService = inject(LanguageService);
  private userService = inject(UserService);
  private signalR = inject(DashboardSignalRService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;
  private signalRSub!: Subscription;

  mathMin = Math.min;

  // Icons
  Activity = Activity;
  Users = Users;
  ShieldAlert = ShieldAlert;
  FileText = FileText;
  Monitor = Monitor;
  CheckCircle = CheckCircle;
  TrendingUp = TrendingUp;
  AlertTriangle = AlertTriangle;
  Search = Search;
  DollarSign = DollarSign;
  BookOpen = BookOpen;
  FileStack = FileStack;
  FileEdit = FileEdit;
  CheckSquare = CheckSquare;
  GraduationCap = GraduationCap;
  UserCheck = UserCheck;
  Award = Award;
  Target = Target;

  // State
  loading = signal<boolean>(true);
  error = signal<string>('');
  dashboardData = signal<AdminDashboardResponse>({
    totalActiveCourses: 0,
    totalExams: 0,
    totalCompletedExams: 0,
    totalSubmissions: 0,
    totalViolations: 0,
    totalStudents: 0,
    totalTutors: 0,
    activeExamsInProgress: 0,
    averagePassRate: 0,
    forceSubmissionRate: 0,
    totalRevenueUSD: 0,
    violationsByCourse: [],
    globalSubmissionOutcomes: [],
    recentPayments: [],
    topViolationTypes: [],
    activityTrend: [],
    activeCourseTitles: [],
    courseViolationDetails: [],
    courseSubmissionOutcomes: [],
    examStatistics: [],
    examStatisticsTotalCount: 0,
    examStatisticsPage: 1,
    examStatisticsPageSize: 10,
    examStatisticsTotalPages: 0
  });

  // Dynamic Chart States
  activityDays = signal<number | null>(null);
  paymentsDays = signal<number | null>(null);
  
  platformActivityData = signal<DailyActivityPoint[]>([]);
  paymentsTrendData = signal<PaymentTrendPoint[]>([]);
  
  activityLoading = signal<boolean>(true);
  paymentsLoading = signal<boolean>(true);

  // Course Filters for Charts
  violationsSeverityCourseFilter = signal<string>('All');
  topViolationsCourseFilter = signal<string>('All');
  submissionOutcomesCourseFilter = signal<string>('All');

  coursesWithViolations = computed<string[]>(() => {
    const data = this.dashboardData()?.courseViolationDetails || [];
    return Array.from(new Set(data.map(d => d.courseTitle || 'Other'))).sort();
  });

  coursesWithOutcomes = computed<string[]>(() => {
    const data = this.dashboardData()?.courseSubmissionOutcomes || [];
    return Array.from(new Set(data.map(d => d.courseTitle || 'Other'))).sort();
  });

  // Table State
  searchQuery = signal<string>('');
  tutorId = signal<string>('');
  tutors = signal<any[]>([]);
  sortColumn = signal<string>('ScheduledAt');
  sortDirection = signal<'asc' | 'desc'>('desc');
  currentPage = signal<number>(1);
  pageSize = signal<number>(10);

  // Charts Computed Signals
  trendChartOptions = computed<EChartsOption>(() => {
    const data = this.platformActivityData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || data.length === 0) {
      return {};
    }
    const dates = data.map(d => d.date);
    const exams = data.map(d => d.examCount);
    const violations = data.map(d => d.violationCount);

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#94a3b8' : '#64748b';
    const lineColor = isDark ? '#334155' : '#cbd5e1';
    const splitLineColor = isDark ? '#1e293b' : '#f1f5f9';

    return {
      tooltip: { 
        trigger: 'axis',
        formatter: (params: any) => {
          let title = params[0]?.name || '';
          if (title) {
            const parts = title.split('-');
            if (parts.length === 3) title = `${parts[2]}-${parts[1]}-${parts[0]}`;
          }
          let res = `<b>${title}</b><br/>`;
          params.forEach((p: any) => {
            res += `${p.marker} ${p.seriesName}: <b>${p.value}</b><br/>`;
          });
          return res;
        }
      },
      legend: { 
        data: [this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_EXAMS'), this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_VIOLATIONS')], 
        bottom: 0,
        textStyle: { color: textColor }
      },
      grid: { left: '3%', right: '8%', bottom: '15%', top: '10%', containLabel: true },
      xAxis: { 
        type: 'category', 
        boundaryGap: false, 
        data: dates,
        axisLine: { lineStyle: { color: lineColor } },
        axisLabel: { 
          color: textColor,
          formatter: (value: string) => {
            if (!value) return '';
            const parts = value.split('-');
            if (parts.length === 3) return `${parts[2]}-${parts[1]}-${parts[0]}`;
            return value;
          }
        }
      },
      yAxis: { 
        type: 'value', 
        axisLine: { show: false }, 
        axisLabel: { color: textColor },
        splitLine: { lineStyle: { color: splitLineColor, type: 'dashed' } }
      },
      series: [
        {
          name: this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_EXAMS'),
          type: 'line',
          smooth: true,
          data: exams,
          itemStyle: { color: '#3b82f6' },
          areaStyle: {
            color: {
              type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: isDark ? 'rgba(59, 130, 246, 0.15)' : 'rgba(59, 130, 246, 0.3)' },
                { offset: 1, color: 'rgba(59, 130, 246, 0.01)' }
              ]
            }
          }
        },
        {
          name: this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_VIOLATIONS'),
          type: 'line',
          smooth: true,
          data: violations,
          itemStyle: { color: '#ef4444' }
        }
      ]
    };
  });

  gaugeChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    if (!data) return {};
    const rate = data.forceSubmissionRate || 0;
    
    return {
      series: [
        {
          type: 'gauge',
          startAngle: 180,
          endAngle: 0,
          min: 0,
          max: 100,
          splitNumber: 4,
          axisLine: {
            lineStyle: {
              width: 10,
              color: [
                [0.3, '#10b981'],
                [0.7, '#f59e0b'],
                [1, '#ef4444']
              ]
            }
          },
          pointer: { show: true, length: '70%', width: 5 },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: { show: false },
          detail: {
            valueAnimation: true,
            formatter: '{value}%',
            color: 'inherit',
            fontSize: 20,
            offsetCenter: [0, '70%']
          },
          data: [{ value: rate }]
        }
      ]
    };
  });

  topViolationsChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    
    let details = data?.courseViolationDetails || [];
    if (this.topViolationsCourseFilter() !== 'All') {
      details = details.filter(d => (d.courseTitle || 'Other') === this.topViolationsCourseFilter());
    }

    if (details.length === 0) {
      return {};
    }

    const typeMap = new Map<string, number>();
    details.forEach(d => {
      typeMap.set(d.violationType, (typeMap.get(d.violationType) || 0) + d.count);
    });

    const topTypes = Array.from(typeMap.entries())
      .map(([violationType, count]) => ({ violationType, count }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 10)
      .reverse();

    if (topTypes.length === 0) return {};

    const types = topTypes.map(t => t.violationType);
    const counts = topTypes.map(t => t.count);

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#94a3b8' : '#475569';
    const labelColor = isDark ? '#94a3b8' : '#64748b';
    const total = counts.reduce((a, b) => a + b, 0);

    return {
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '5%', right: '5%', bottom: '15%', top: '10%', containLabel: true },
      xAxis: { 
        type: 'category', 
        data: types,
        axisLine: { lineStyle: { color: activeTheme === 'dark' ? '#334155' : '#e2e8f0' } },
        axisLabel: { 
          color: textColor, 
          fontWeight: 'bold',
          fontSize: 11,
          rotate: 30
        }
      },
      yAxis: { 
        type: 'value', 
        axisLine: { show: false }, 
        splitLine: { lineStyle: { color: activeTheme === 'dark' ? '#334155' : '#e2e8f0', type: 'dashed' } }
      },
      series: [
        {
          name: this.translate.instant('ADMIN_DASHBOARD.CHART_SERIES_COUNT'),
          type: 'bar' as const,
          data: counts,
          itemStyle: { 
            color: {
              type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [{ offset: 0, color: '#f97316' }, { offset: 1, color: '#fdba74' }]
            },
            borderRadius: [4, 4, 0, 0] 
          },
          label: { show: true, position: 'top', color: labelColor, fontSize: 11 },
          barMaxWidth: 40
        }
      ]
    };
  });

  courseViolationsChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || !data.violationsByCourse || data.violationsByCourse.length === 0) {
      return {};
    }

    const courseViolations = data.violationsByCourse.map(v => ({ name: v.courseTitle, value: v.violationCount }));
    const isDark = activeTheme === 'dark';
    const labelColor = isDark ? '#e2e8f0' : '#334155';
    const borderColor = isDark ? '#1e293b' : '#ffffff';

    return {
      tooltip: { 
        trigger: 'item',
        formatter: '{a} <br/>{b}: <b>{c}</b> Violations ({d}%)',
        confine: true
      },
      legend: { 
        type: 'scroll',
        orient: 'horizontal',
        bottom: 0,
        left: 'center',
        textStyle: { color: labelColor },
        formatter: (name: string) => name.length > 25 ? name.substring(0, 25) + '...' : name,
        tooltip: { show: true, confine: true }
      },
      series: [
        {
          name: this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_VIOLATIONS'),
          type: 'pie' as const,
          radius: ['40%', '70%'],
          center: ['50%', '45%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 8, borderColor: borderColor, borderWidth: 2 },
          label: { 
            show: true, 
            formatter: '{b}\n{d}%',
            color: labelColor,
            position: 'outside'
          },
          labelLine: { show: true },
          emphasis: {
            label: { show: true, fontSize: 12, fontWeight: 'bold' }
          },
          data: courseViolations
        }
      ]
    };
  });

  violationsSeverityChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    
    let details = data?.courseViolationDetails || [];
    if (this.violationsSeverityCourseFilter() !== 'All') {
      details = details.filter(d => (d.courseTitle || 'Other') === this.violationsSeverityCourseFilter());
    }

    if (details.length === 0) {
      return {};
    }

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#e2e8f0' : '#334155';
    const borderColor = isDark ? '#1e293b' : '#ffffff';

    let totalCritical = 0;
    let totalMedium = 0;
    let totalMinor = 0;

    details.forEach(d => {
      if (d.severity === 'Critical') totalCritical += d.count;
      else if (d.severity === 'Medium') totalMedium += d.count;
      else if (d.severity === 'Minor') totalMinor += d.count;
    });

    const pieData = [
      { name: this.translate.instant('ADMIN_DASHBOARD.SEVERITY_CRITICAL') || 'Critical', value: totalCritical, itemStyle: { color: '#ef4444' } },
      { name: this.translate.instant('ADMIN_DASHBOARD.SEVERITY_MEDIUM') || 'Medium', value: totalMedium, itemStyle: { color: '#f59e0b' } },
      { name: this.translate.instant('ADMIN_DASHBOARD.SEVERITY_MINOR') || 'Minor', value: totalMinor, itemStyle: { color: '#10b981' } }
    ].filter(d => d.value > 0);

    const totalViolations = totalCritical + totalMedium + totalMinor;

    return {
      title: {
        text: totalViolations.toString(),
        subtext: this.translate.instant('ADMIN_DASHBOARD.KPI_VIOLATIONS') || 'Violations',
        left: 'center',
        top: '32%',
        textStyle: { fontSize: 22, fontWeight: 'bold', color: textColor },
        subtextStyle: { fontSize: 12, color: isDark ? '#94a3b8' : '#64748b' }
      },
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)', confine: true },
      legend: { bottom: '5%', icon: 'circle', textStyle: { color: textColor, fontSize: 11 } },
      series: [
        {
          name: 'Severity',
          type: 'pie',
          radius: ['50%', '75%'],
          center: ['50%', '42%'],
          avoidLabelOverlap: false,
          itemStyle: { borderRadius: 6, borderColor: borderColor, borderWidth: 2 },
          label: { show: false },
          emphasis: {
            label: { show: false }
          },
          data: pieData
        }
      ]
    };
  });

  submissionOutcomesChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    
    let outcomes = data?.courseSubmissionOutcomes || [];
    if (this.submissionOutcomesCourseFilter() !== 'All') {
      outcomes = outcomes.filter(o => (o.courseTitle || 'Other') === this.submissionOutcomesCourseFilter());
    }

    if (outcomes.length === 0) {
      return {};
    }

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#e2e8f0' : '#334155';
    const borderColor = isDark ? '#1e293b' : '#ffffff';

    const outcomeMap = new Map<string, number>();
    outcomes.forEach(o => {
      outcomeMap.set(o.outcome, (outcomeMap.get(o.outcome) || 0) + o.count);
    });

    const pieData = Array.from(outcomeMap.entries())
      .map(([outcome, count]) => {
        let name = outcome;
        let color = '#94a3b8';
        
        if (outcome === 'Submitted') { name = this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_NORMAL') || 'Normal'; color = '#10b981'; }
        if (outcome === 'ForceSubmitted') { name = this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_FORCE_TIMEOUT') || 'Forced (Timeout)'; color = '#f59e0b'; }
        if (outcome === 'AutoExpired') { name = this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_FORCE_VIOLATIONS') || 'Forced (Violations)'; color = '#ef4444'; }
        if (outcome === 'InProgress') { name = this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_ACTIVE') || 'Active'; color = '#3b82f6'; }

        return { name, value: count, itemStyle: { color } };
      })
      .filter(d => d.value > 0);

    const totalAttempts = pieData.reduce((acc, curr) => acc + curr.value, 0);

    if (totalAttempts === 0) return {};

    return {
      title: {
        text: totalAttempts.toString(),
        subtext: this.translate.instant('ADMIN_DASHBOARD.COL_ATTEMPTS') || 'Attempts',
        left: 'center',
        top: '38%',
        textStyle: { fontSize: 22, fontWeight: 'bold', color: textColor },
        subtextStyle: { fontSize: 12, color: isDark ? '#94a3b8' : '#64748b' }
      },
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)', confine: true },
      legend: { 
        type: 'scroll',
        bottom: '0%', 
        icon: 'circle', 
        textStyle: { color: textColor },
        formatter: (name: string) => name.length > 25 ? name.substring(0, 25) + '...' : name,
        tooltip: { show: true, confine: true }
      },
      series: [
        {
          name: 'Outcome',
          type: 'pie',
          radius: ['50%', '70%'],
          center: ['50%', '45%'],
          avoidLabelOverlap: false,
          itemStyle: { borderRadius: 8, borderColor: borderColor, borderWidth: 2 },
          label: { show: false },
          emphasis: {
            label: { show: false }
          },
          data: pieData
        }
      ]
    };
  });


  recentPaymentsChartOptions = computed<EChartsOption>(() => {
    const data = this.paymentsTrendData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || data.length === 0) {
      return {};
    }

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#e2e8f0' : '#334155';
    const axisLineColor = isDark ? '#334155' : '#e2e8f0';

    const dates = data.map(p => {
      const d = new Date(p.date);
      const dd = d.getDate().toString().padStart(2, '0');
      const mm = (d.getMonth() + 1).toString().padStart(2, '0');
      const yyyy = d.getFullYear();
      return `${dd}/${mm}/${yyyy}`;
    });
    const amounts = data.map(p => p.amountUSD);

    return {
      tooltip: { 
        trigger: 'axis',
        formatter: (params: any) => {
          const idx = params[0].dataIndex;
          return `Date: ${dates[idx]}<br/>Amount: <b>$${amounts[idx]}</b>`;
        }
      },
      grid: {
        left: '5%',
        right: '5%',
        bottom: '15%',
        top: '10%',
        containLabel: true
      },
      xAxis: {
        type: 'category',
        data: dates,
        axisLine: { lineStyle: { color: axisLineColor } },
        axisLabel: { 
          color: textColor, 
          rotate: 30, 
          fontSize: 10,
          formatter: (value: string) => value
        }
      },
      yAxis: {
        type: 'value',
        axisLine: { show: false },
        splitLine: { lineStyle: { color: axisLineColor, type: 'dashed' } },
        axisLabel: { color: textColor, formatter: '${value}' }
      },
      series: [
        {
          name: 'Payment',
          type: 'line',
          smooth: true,
          data: amounts,
          areaStyle: {
            color: {
              type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [{ offset: 0, color: 'rgba(16, 185, 129, 0.4)' }, { offset: 1, color: 'rgba(16, 185, 129, 0.0)' }]
            }
          },
          itemStyle: {
            color: '#10b981'
          }
        }
      ]
    };
  });

  ngOnInit() {
    this.userService.getTutors().subscribe({
      next: (res) => this.tutors.set(res),
      error: (err) => console.error('Failed to load tutors', err)
    });
    this.loadDashboard();
    this.loadActivityData();
    this.loadPaymentsData();

    this.langSub = this.languageService.languageChange$.subscribe(() => {
      this.loadDashboard();
      this.loadActivityData();
      this.loadPaymentsData();
    });

    this.signalR.startConnection();
    this.signalRSub = this.signalR.dashboardUpdated$.subscribe(() => {
      this.loadDashboardSilent();
    });
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
    this.signalRSub?.unsubscribe();
    this.signalR.stopConnection();
  }

  loadDashboardSilent() {
    this.monitoring.getAdminDashboard(
      this.currentPage(),
      this.pageSize(),
      this.searchQuery(),
      this.tutorId(),
      this.sortColumn(),
      this.sortDirection()
    ).subscribe({
      next: (res) => {
        if (res.data) {
          this.dashboardData.set(res.data);
        }
      }
    });
  }

  loadDashboard() {
    this.loading.set(true);

    this.monitoring.getAdminDashboard(
      this.currentPage(),
      this.pageSize(),
      this.searchQuery(),
      this.tutorId(),
      this.sortColumn(),
      this.sortDirection()
    ).subscribe({
      next: (res) => {
        if (res.data) {
          this.dashboardData.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.translate.instant('ADMIN_DASHBOARD.ERR_LOAD'));
        this.loading.set(false);
      }
    });
  }

  onSearch(query: string) {
    this.searchQuery.set(query);
    this.currentPage.set(1);
    this.loadDashboard();
  }

  onTutorChange(tutorId: string) {
    this.tutorId.set(tutorId);
    this.currentPage.set(1);
    this.loadDashboard();
  }

  onSort(column: string) {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('desc');
    }
    this.currentPage.set(1);
    this.loadDashboard();
  }

  onPageChange(page: number) {
    if (page >= 1 && page <= this.dashboardData().examStatisticsTotalPages) {
      this.currentPage.set(page);
      this.loadDashboard();
    }
  }

  trackByExamId(index: number, row: ExamStatisticsRow) { return row.examId; }

  loadActivityData() {
    this.activityLoading.set(true);
    this.monitoring.getPlatformActivity(this.activityDays()).subscribe({
      next: (res) => {
        if (res.data) {
          this.platformActivityData.set(res.data.activityTrend);
        }
        this.activityLoading.set(false);
      },
      error: () => {
        this.activityLoading.set(false);
      }
    });
  }

  loadPaymentsData() {
    this.paymentsLoading.set(true);
    this.monitoring.getPaymentsTrend(this.paymentsDays()).subscribe({
      next: (res) => {
        if (res.data) {
          this.paymentsTrendData.set(res.data.paymentsTrend);
        }
        this.paymentsLoading.set(false);
      },
      error: () => {
        this.paymentsLoading.set(false);
      }
    });
  }

  onActivityDaysChange(event: any) {
    const value = event.target.value;
    this.activityDays.set(value ? parseInt(value) : null);
    this.loadActivityData();
  }

  onPaymentsDaysChange(event: any) {
    const value = event.target.value;
    this.paymentsDays.set(value ? parseInt(value) : null);
    this.loadPaymentsData();
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
    csvContent += `Total Tutors,${data.totalTutors}\n`;
    csvContent += `Total Exams,${data.totalExams}\n`;
    csvContent += `Completed Exams,${data.totalCompletedExams}\n`;
    csvContent += `Active Exams In Progress,${data.activeExamsInProgress}\n`;
    csvContent += `Total Submissions,${data.totalSubmissions}\n`;
    csvContent += `Total Violations,${data.totalViolations}\n`;
    csvContent += `Average Pass Rate,${data.averagePassRate}%\n`;
    csvContent += `Force Submission Rate,${data.forceSubmissionRate}%\n`;
    csvContent += `Total Revenue (USD),$${data.totalRevenueUSD}\n\n`;

    // 2. Exam Statistics Table
    csvContent += "=== EXAM STATISTICS ===\n";
    csvContent += "Exam Title,Course,Tutor,Scheduled,Attempts,Submitted,Force Submitted,In Progress,Total Violations,Avg Score,Pass Rate\n";
    if (data.examStatistics) {
      data.examStatistics.forEach(e => {
        csvContent += `"${e.examTitle}","${e.courseTitle}","${e.tutorName}","${e.scheduledAt || 'N/A'}",${e.totalAttempts},${e.submittedCount},${e.forceSubmittedCount},${e.inProgressCount},${e.totalViolations},${e.averageScore || 0},${e.passRate || 0}%\n`;
      });
    }
    csvContent += "\n";

    // 3. Course Violation Details
    csvContent += "=== COURSE VIOLATIONS DETAILS ===\n";
    csvContent += "Course,Violation Type,Severity,Count\n";
    if (data.courseViolationDetails) {
      data.courseViolationDetails.forEach(v => {
        csvContent += `"${v.courseTitle}","${v.violationType}","${v.severity}",${v.count}\n`;
      });
    }
    csvContent += "\n";

    // 4. Violations By Course Summary
    csvContent += "=== VIOLATIONS BY COURSE SUMMARY ===\n";
    csvContent += "Course,Total Violations,Critical,Medium,Minor\n";
    if (data.violationsByCourse) {
      data.violationsByCourse.forEach(v => {
        csvContent += `"${v.courseTitle}",${v.violationCount},${v.criticalCount},${v.mediumCount},${v.minorCount}\n`;
      });
    }
    csvContent += "\n";

    // 5. Global Submission Outcomes
    csvContent += "=== GLOBAL SUBMISSION OUTCOMES ===\n";
    csvContent += "Outcome,Count,Percentage\n";
    if (data.globalSubmissionOutcomes) {
      data.globalSubmissionOutcomes.forEach(o => {
        csvContent += `"${o.outcome}",${o.count},${o.percentage}%\n`;
      });
    }
    csvContent += "\n";

    // 6. Top Violation Types
    csvContent += "=== TOP VIOLATION TYPES ===\n";
    csvContent += "Violation Type,Count\n";
    if (data.topViolationTypes) {
      data.topViolationTypes.forEach(t => {
        csvContent += `"${t.violationType}",${t.count}\n`;
      });
    }
    csvContent += "\n";

    // 7. Recent Payments
    csvContent += "=== RECENT PAYMENTS ===\n";
    csvContent += "Payment ID,Amount (USD),Paid At,Student Name\n";
    if (data.recentPayments) {
      data.recentPayments.forEach(p => {
        csvContent += `"${p.paymentId}",$${p.amountUSD},"${p.paidAt}","${p.studentName}"\n`;
      });
    }
    csvContent += "\n";

    // 8. Activity Trend
    csvContent += "=== ACTIVITY TREND ===\n";
    csvContent += "Date,Exam Count,Violation Count\n";
    if (data.activityTrend) {
      data.activityTrend.forEach(a => {
        csvContent += `"${a.date}",${a.examCount},${a.violationCount}\n`;
      });
    }

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", "Admin_Dashboard_Report.csv");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
