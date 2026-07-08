import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Activity, Users, ShieldAlert, FileText, Monitor, CheckCircle, TrendingUp, AlertTriangle, Search, DollarSign } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, AdminDashboardResponse, ExamStatisticsRow } from '../../../core/services/monitoring.service';
import { ThemeService } from '../../../core/services/theme.service';
import { LanguageService } from '../../../core/services/language.service';
import { UserService } from '../../../core/services/user.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

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
  public translate = inject(TranslateService);
  private langSub!: Subscription;

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

  // State
  loading = signal<boolean>(true);
  error = signal<string>('');
  dashboardData = signal<AdminDashboardResponse>({
    totalActiveCourses: 0,
    totalCompletedExams: 0,
    totalSubmissions: 0,
    totalViolations: 0,
    totalStudents: 0,
    totalTutors: 0,
    activeExamsInProgress: 0,
    forceSubmissionRate: 0,
    totalRevenueUSD: 0,
    violationsByCourse: [],
    globalSubmissionOutcomes: [],
    recentPayments: [],
    topViolationTypes: [],
    activityTrend: [],
    examStatistics: [],
    examStatisticsTotalCount: 0,
    examStatisticsPage: 1,
    examStatisticsPageSize: 10,
    examStatisticsTotalPages: 0
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
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || !data.activityTrend || data.activityTrend.length === 0) {
      return {};
    }
    const dates = data.activityTrend.map(d => d.date);
    const exams = data.activityTrend.map(d => d.examCount);
    const violations = data.activityTrend.map(d => d.violationCount);

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#94a3b8' : '#64748b';
    const lineColor = isDark ? '#334155' : '#cbd5e1';
    const splitLineColor = isDark ? '#1e293b' : '#f1f5f9';

    return {
      tooltip: { trigger: 'axis' },
      legend: { 
        data: [this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_EXAMS'), this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_VIOLATIONS')], 
        bottom: 0,
        textStyle: { color: textColor }
      },
      grid: { left: '3%', right: '4%', bottom: '15%', top: '10%', containLabel: true },
      xAxis: { 
        type: 'category', 
        boundaryGap: false, 
        data: dates,
        axisLine: { lineStyle: { color: lineColor } },
        axisLabel: { color: textColor }
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
    if (!data || !data.topViolationTypes || data.topViolationTypes.length === 0) {
      return {};
    }
    const types = [...data.topViolationTypes].reverse().map(t => t.violationType);
    const counts = [...data.topViolationTypes].reverse().map(t => t.count);

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
          type: 'bar',
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
        formatter: '{a} <br/>{b}: <b>{c}</b> ({d}%)'
      },
      legend: { 
        type: 'scroll',
        orient: 'horizontal',
        bottom: 0,
        left: 'center',
        textStyle: { color: labelColor }
      },
      series: [
        {
          name: this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_VIOLATIONS'),
          type: 'pie',
          radius: ['40%', '70%'],
          center: ['50%', '45%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 8, borderColor: borderColor, borderWidth: 2 },
          label: { show: false }, // Hide labels to rely on legend
          emphasis: {
            label: {
              show: true,
              fontSize: 14,
              fontWeight: 'bold',
              color: labelColor
            }
          },
          data: courseViolations
        }
      ]
    };
  });

  violationsSeverityChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || !data.violationsByCourse || data.violationsByCourse.length === 0) {
      return {};
    }

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#e2e8f0' : '#334155';
    const borderColor = isDark ? '#1e293b' : '#ffffff';

    const totalCritical = data.violationsByCourse.reduce((acc, v) => acc + v.criticalCount, 0);
    const totalMedium = data.violationsByCourse.reduce((acc, v) => acc + v.mediumCount, 0);
    const totalMinor = data.violationsByCourse.reduce((acc, v) => acc + v.minorCount, 0);

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
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
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
            label: { show: true, fontSize: 14, fontWeight: 'bold', color: textColor }
          },
          data: pieData
        }
      ]
    };
  });

  submissionOutcomesChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || !data.globalSubmissionOutcomes || data.globalSubmissionOutcomes.length === 0) {
      return {};
    }

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#e2e8f0' : '#334155';
    const borderColor = isDark ? '#1e293b' : '#ffffff';

    const pieData = data.globalSubmissionOutcomes
      .filter(o => o.outcome !== 'AutoExpired')
      .map(o => {
        let name = o.outcome;
        let color = '#94a3b8';
        
        if (o.outcome === 'Submitted') { name = this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_NORMAL') || 'Normal'; color = '#10b981'; }
        if (o.outcome === 'ForceSubmitted') { name = this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_FORCE_SUBMIT') || 'Force Submitted'; color = '#f59e0b'; }
        if (o.outcome === 'InProgress') { name = this.translate.instant('ADMIN_DASHBOARD.CHART_LEGEND_ACTIVE') || 'Active'; color = '#3b82f6'; }

        return { name, value: o.count, itemStyle: { color } };
      });

    const totalAttempts = pieData.reduce((acc, curr) => acc + curr.value, 0);

    return {
      title: {
        text: totalAttempts.toString(),
        subtext: this.translate.instant('ADMIN_DASHBOARD.COL_ATTEMPTS') || 'Attempts',
        left: 'center',
        top: '38%',
        textStyle: { fontSize: 22, fontWeight: 'bold', color: textColor },
        subtextStyle: { fontSize: 12, color: isDark ? '#94a3b8' : '#64748b' }
      },
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
      legend: { bottom: '5%', icon: 'circle', textStyle: { color: textColor } },
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
            label: { show: true, fontSize: 16, fontWeight: 'bold', color: textColor }
          },
          data: pieData
        }
      ]
    };
  });


  recentPaymentsChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || !data.recentPayments || data.recentPayments.length === 0) {
      return {};
    }

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#e2e8f0' : '#334155';
    const axisLineColor = isDark ? '#334155' : '#e2e8f0';

    // Sort ascending by date for left-to-right timeline
    const sortedPayments = [...data.recentPayments].sort((a, b) => new Date(a.paidAt).getTime() - new Date(b.paidAt).getTime());
    
    const dates = sortedPayments.map(p => {
      const d = new Date(p.paidAt);
      const dd = d.getDate().toString().padStart(2, '0');
      const mm = (d.getMonth() + 1).toString().padStart(2, '0');
      const yyyy = d.getFullYear();
      const hh = d.getHours().toString().padStart(2, '0');
      const min = d.getMinutes().toString().padStart(2, '0');
      const ss = d.getSeconds().toString().padStart(2, '0');
      return `${dd}/${mm}/${yyyy}\n${hh}:${min}:${ss}`;
    });
    const amounts = sortedPayments.map(p => p.amountUSD);
    const names = sortedPayments.map(p => p.studentName);

    return {
      tooltip: { 
        trigger: 'axis',
        formatter: (params: any) => {
          const idx = params[0].dataIndex;
          return `<b>${names[idx]}</b><br/>Amount: $${amounts[idx]}<br/>Date: ${dates[idx].replace('\n', ' ')}`;
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
          formatter: (value: string) => {
            const parts = value.split('\n');
            const datePart = parts[0].substring(0, 5);
            const timePart = parts[1].substring(0, 5);
            return `${datePart} ${timePart}`;
          }
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
    this.langSub = this.languageService.languageChange$.subscribe(() => this.loadDashboard());
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
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
}
