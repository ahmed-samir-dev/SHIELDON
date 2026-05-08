import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Activity, Users, ShieldAlert, FileText, Monitor, CheckCircle, TrendingUp, AlertTriangle } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, AdminDashboardResponse, ExamStatisticsRow } from '../../../core/services/monitoring.service';

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

  // Charts
  trendChartOptions = signal<EChartsOption>({});
  topViolationsChartOptions = signal<EChartsOption>({});
  gaugeChartOptions = signal<EChartsOption>({});
  courseViolationsChartOptions = signal<EChartsOption>({});
  submissionOutcomesChartOptions = signal<EChartsOption>({});

  // Chart equality guards — prevent re-animation when data hasn't changed
  private lastTrendKey = '';
  private lastTopViolationsKey = '';
  private lastGaugeValue = -1;
  private lastCourseViolationsKey = '';
  private lastSubmissionOutcomesKey = '';

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);

    this.monitoring.getAdminDashboard().subscribe({
      next: (res) => {
        if (res.data) {
          this.dashboardData.set(res.data);
          this.initCharts(res.data);
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load admin dashboard data.');
        this.loading.set(false);
      }
    });
  }

  private initCharts(data: AdminDashboardResponse) {
    // 1. Line Chart: 30-Day Trend
    if (data.activityTrend && data.activityTrend.length > 0) {
      const trendKey = JSON.stringify(data.activityTrend);
      if (trendKey !== this.lastTrendKey) {
        this.lastTrendKey = trendKey;
        const dates = data.activityTrend.map(d => d.date);
        const exams = data.activityTrend.map(d => d.examCount);
        const violations = data.activityTrend.map(d => d.violationCount);

        this.trendChartOptions.set({
        tooltip: { trigger: 'axis' },
        legend: { data: ['Exams Taken', 'Violations'], bottom: 0 },
        grid: { left: '3%', right: '4%', bottom: '15%', top: '10%', containLabel: true },
        xAxis: { 
          type: 'category', 
          boundaryGap: false, 
          data: dates,
          axisLine: { lineStyle: { color: '#cbd5e1' } },
          axisLabel: { color: '#64748b' }
        },
        yAxis: { type: 'value', axisLine: { show: false }, splitLine: { lineStyle: { color: '#f1f5f9', type: 'dashed' } } },
        series: [
          {
            name: 'Exams Taken',
            type: 'line',
            smooth: true,
            data: exams,
            itemStyle: { color: '#3b82f6' }, // blue-500
            areaStyle: {
              color: {
                type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
                colorStops: [{ offset: 0, color: 'rgba(59, 130, 246, 0.3)' }, { offset: 1, color: 'rgba(59, 130, 246, 0.05)' }]
              }
            }
          },
          {
            name: 'Violations',
            type: 'line',
            smooth: true,
            data: violations,
            itemStyle: { color: '#ef4444' }, // red-500
          }
          ]
        });
      }
    }

    // 2. Horizontal Bar: Top Violation Types
    if (data.topViolationTypes && data.topViolationTypes.length > 0) {
      const topKey = JSON.stringify(data.topViolationTypes);
      if (topKey !== this.lastTopViolationsKey) {
        this.lastTopViolationsKey = topKey;
        const types = [...data.topViolationTypes].reverse().map(t => t.violationType);
        const counts = [...data.topViolationTypes].reverse().map(t => t.count);

        this.topViolationsChartOptions.set({
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        grid: { left: '3%', right: '12%', bottom: '5%', top: '5%', containLabel: true },
        xAxis: { type: 'value', show: false },
        yAxis: { 
          type: 'category', 
          data: types, 
          axisLine: { show: false }, 
          axisTick: { show: false },
          axisLabel: { 
            color: '#475569', 
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
              label: { show: true, position: 'right', color: '#64748b', fontSize: 11 }
            }
          ]
        });
      }
    }

    // 3. Gauge: Suspicious Submission Rate
    const gaugeValue = data.forceSubmissionRate;
    if (gaugeValue !== this.lastGaugeValue) {
      this.lastGaugeValue = gaugeValue;
      this.gaugeChartOptions.set({
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
            progress: { show: true, roundCap: true, width: 18 },
            pointer: { show: false },
            axisLine: { roundCap: true, lineStyle: { width: 18 } },
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
              color: '#1e293b'
            },
            data: [{ value: gaugeValue }]
          }
        ]
      });
    }

    // 4. Doughnut: Violations by Course
    if (data.examStatistics && data.examStatistics.length > 0) {
      // Group by course title and sum violations
      const courseMap = new Map<string, number>();
      data.examStatistics.forEach(stat => {
        if (stat.totalViolations > 0) {
          const current = courseMap.get(stat.courseTitle) || 0;
          courseMap.set(stat.courseTitle, current + stat.totalViolations);
        }
      });

      const courseViolations = Array.from(courseMap.entries()).map(([name, value]) => ({ name, value }));
      const courseViolationsKey = JSON.stringify(courseViolations);

      if (courseViolationsKey !== this.lastCourseViolationsKey) {
        this.lastCourseViolationsKey = courseViolationsKey;
        this.courseViolationsChartOptions.set({
          tooltip: { trigger: 'item' },
          legend: { show: false },
          series: [
            {
              name: 'Violations',
              type: 'pie',
              radius: ['40%', '70%'],
              center: ['50%', '50%'],
              avoidLabelOverlap: true,
              itemStyle: { borderRadius: 8, borderColor: '#fff', borderWidth: 2 },
              label: { show: true, formatter: '{b}: {c}', color: '#475569', fontSize: 11 },
              data: courseViolations
            }
          ]
        });
      }

      // 5. Doughnut: Global Submission Outcomes
      let sumSubmitted = 0;
      let sumForceSubmitted = 0;
      let sumInProgress = 0;
      data.examStatistics.forEach(s => {
        sumSubmitted += s.submittedCount;
        sumForceSubmitted += s.forceSubmittedCount;
        sumInProgress += s.inProgressCount;
      });

      const outcomesKey = `${sumSubmitted}-${sumForceSubmitted}-${sumInProgress}`;
      if (outcomesKey !== this.lastSubmissionOutcomesKey) {
        this.lastSubmissionOutcomesKey = outcomesKey;
        this.submissionOutcomesChartOptions.set({
          tooltip: { trigger: 'item' },
          legend: { bottom: '5%', icon: 'circle', textStyle: { color: '#64748b' } },
          series: [
            {
              name: 'Outcome',
              type: 'pie',
              radius: ['50%', '70%'],
              center: ['50%', '45%'],
              avoidLabelOverlap: false,
              itemStyle: { borderRadius: 8, borderColor: '#fff', borderWidth: 2 },
              label: { show: false },
              data: [
                { value: sumSubmitted, name: 'Normal', itemStyle: { color: '#10b981' } },
                { value: sumInProgress, name: 'Active', itemStyle: { color: '#3b82f6' } },
                { value: sumForceSubmitted, name: 'Terminated', itemStyle: { color: '#ef4444' } }
              ]
            }
          ]
        });
      }
    }
  }

  trackByExamId(index: number, row: ExamStatisticsRow) { return row.examId; }
}
