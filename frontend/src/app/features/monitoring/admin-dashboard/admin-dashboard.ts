import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Activity, Users, ShieldAlert, FileText, Monitor, CheckCircle, TrendingUp, AlertTriangle } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, AdminDashboardResponse, ExamStatisticsRow } from '../../../core/services/monitoring.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, NgxEchartsModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.scss']
})
export class AdminDashboardComponent implements OnInit {
  private monitoring = inject(MonitoringService);
  private router = inject(Router);
  private themeService = inject(ThemeService);

  // Icons
  Activity = Activity;
  Users = Users;
  ShieldAlert = ShieldAlert;
  FileText = FileText;
  Monitor = Monitor;
  CheckCircle = CheckCircle;
  TrendingUp = TrendingUp;
  AlertTriangle = AlertTriangle;

  // State
  loading = signal<boolean>(true);
  error = signal<string>('');
  dashboardData = signal<AdminDashboardResponse>({
    totalActiveCourses: 0,
    totalCompletedExams: 0,
    totalSubmissions: 0,
    totalViolations: 0,
    forceSubmissionRate: 0,
    examStatistics: [],
    topViolationTypes: [],
    activityTrend: []
  });

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
        data: ['Exams Taken', 'Violations'], 
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
          name: 'Exams Taken',
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
          name: 'Violations',
          type: 'line',
          smooth: true,
          data: violations,
          itemStyle: { color: '#ef4444' }
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

    return {
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '3%', right: '12%', bottom: '5%', top: '5%', containLabel: true },
      xAxis: { type: 'value', show: false },
      yAxis: { 
        type: 'category', 
        data: types, 
        axisLine: { show: false }, 
        axisTick: { show: false },
        axisLabel: { 
          color: textColor, 
          fontWeight: 'bold',
          fontSize: 11,
          margin: 10
        }
      },
      series: [
        {
          name: 'Count',
          type: 'bar',
          data: counts,
          itemStyle: { 
            color: {
              type: 'linear', x: 0, y: 0, x2: 1, y2: 0,
              colorStops: [{ offset: 0, color: '#f97316' }, { offset: 1, color: '#fdba74' }]
            },
            borderRadius: [0, 4, 4, 0] 
          },
          label: { show: true, position: 'right', color: labelColor, fontSize: 11 }
        }
      ]
    };
  });

  gaugeChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    const gaugeValue = data.forceSubmissionRate;

    const isDark = activeTheme === 'dark';
    const numberColor = isDark ? '#ffffff' : '#1e293b';
    const axisLineBg = isDark ? '#334155' : '#cbd5e1';

    return {
      series: [
        {
          type: 'gauge',
          startAngle: 180,
          endAngle: 0,
          min: 0,
          max: 100,
          splitNumber: 4,
          radius: '90%',
          center: ['50%', '70%'],
          itemStyle: { color: '#ef4444' },
          progress: { 
            show: true, 
            roundCap: true, 
            width: 18,
            itemStyle: {
              color: '#ef4444'
            }
          },
          pointer: { show: false },
          axisLine: { 
            roundCap: true, 
            lineStyle: { 
              width: 18,
              color: [[1, axisLineBg]]
            } 
          },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: { show: false },
          title: { show: false },
          detail: {
            valueAnimation: true,
            offsetCenter: [0, 0],
            fontSize: 24,
            fontWeight: 'bold',
            formatter: '{value}%',
            color: numberColor
          },
          data: [{ value: gaugeValue }]
        }
      ]
    };
  });

  courseViolationsChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || !data.examStatistics || data.examStatistics.length === 0) {
      return {};
    }

    // Group by course title and sum violations
    const courseMap = new Map<string, number>();
    data.examStatistics.forEach(stat => {
      if (stat.totalViolations > 0) {
        const current = courseMap.get(stat.courseTitle) || 0;
        courseMap.set(stat.courseTitle, current + stat.totalViolations);
      }
    });

    const courseViolations = Array.from(courseMap.entries()).map(([name, value]) => ({ name, value }));
    if (courseViolations.length === 0) {
      return {};
    }

    const isDark = activeTheme === 'dark';
    const labelColor = isDark ? '#94a3b8' : '#475569';
    const borderColor = isDark ? '#1e293b' : '#ffffff';

    return {
      tooltip: { trigger: 'item' },
      legend: { show: false },
      series: [
        {
          name: 'Violations',
          type: 'pie',
          radius: ['40%', '70%'],
          center: ['50%', '50%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 8, borderColor: borderColor, borderWidth: 2 },
          label: { show: true, formatter: '{b}: {c}', color: labelColor, fontSize: 11 },
          data: courseViolations
        }
      ]
    };
  });

  submissionOutcomesChartOptions = computed<EChartsOption>(() => {
    const data = this.dashboardData();
    const activeTheme = this.themeService.activeTheme();
    if (!data || !data.examStatistics || data.examStatistics.length === 0) {
      return {};
    }

    let sumSubmitted = 0;
    let sumForceSubmitted = 0;
    let sumInProgress = 0;
    data.examStatistics.forEach(s => {
      sumSubmitted += s.submittedCount;
      sumForceSubmitted += s.forceSubmittedCount;
      sumInProgress += s.inProgressCount;
    });

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#94a3b8' : '#64748b';
    const borderColor = isDark ? '#1e293b' : '#ffffff';

    return {
      tooltip: { trigger: 'item' },
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
          data: [
            { value: sumSubmitted, name: 'Normal', itemStyle: { color: '#10b981' } },
            { value: sumInProgress, name: 'Active', itemStyle: { color: '#3b82f6' } },
            { value: sumForceSubmitted, name: 'Terminated', itemStyle: { color: '#ef4444' } }
          ]
        }
      ]
    };
  });

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);

    this.monitoring.getAdminDashboard().subscribe({
      next: (res) => {
        if (res.data) {
          this.dashboardData.set(res.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load admin dashboard data.');
        this.loading.set(false);
      }
    });
  }

  trackByExamId(index: number, row: ExamStatisticsRow) { return row.examId; }
}
