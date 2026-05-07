import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Activity, Users, ShieldAlert, FileText, Monitor, CheckCircle, TrendingUp, AlertTriangle } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, AdminDashboardResponse, GlobalExamRow } from '../../../core/services/monitoring.service';

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
  dashboardData = signal<AdminDashboardResponse | null>(null);

  // Charts
  trendChartOptions = signal<EChartsOption>({});
  topViolationsChartOptions = signal<EChartsOption>({});
  gaugeChartOptions = signal<EChartsOption>({});

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);
    this.monitoring.getAdminDashboard().subscribe({
      next: (res) => {
        this.dashboardData.set(res.data || null);
        if (res.data) {
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

    // 2. Horizontal Bar: Top Violation Types
    if (data.topViolationTypes && data.topViolationTypes.length > 0) {
      // Reverse to show highest at top
      const types = [...data.topViolationTypes].reverse().map(t => t.violationType);
      const counts = [...data.topViolationTypes].reverse().map(t => t.count);

      this.topViolationsChartOptions.set({
        tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
        grid: { left: '3%', right: '10%', bottom: '3%', top: '5%', containLabel: true },
        xAxis: { type: 'value', show: false },
        yAxis: { 
          type: 'category', 
          data: types, 
          axisLine: { show: false }, 
          axisTick: { show: false },
          axisLabel: { color: '#475569', fontWeight: 'bold' }
        },
        series: [
          {
            name: 'Count',
            type: 'bar',
            data: counts,
            itemStyle: { color: '#f97316', borderRadius: [0, 4, 4, 0] }, // orange-500
            label: { show: true, position: 'right', color: '#64748b' }
          }
        ]
      });
    }

    // 3. Gauge: Suspicious Submission Rate
    this.gaugeChartOptions.set({
      series: [
        {
          type: 'gauge',
          startAngle: 180,
          endAngle: 0,
          min: 0,
          max: 100,
          splitNumber: 4,
          itemStyle: { color: '#ef4444' }, // red-500
          progress: { show: true, roundCap: true, width: 18 },
          pointer: { show: false },
          axisLine: { roundCap: true, lineStyle: { width: 18 } },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: { show: false },
          title: { show: false },
          detail: {
            valueAnimation: true,
            offsetCenter: [0, '-10%'],
            fontSize: 32,
            fontWeight: 'bold',
            formatter: '{value}%',
            color: '#1e293b'
          },
          data: [{ value: data.suspiciousSubmissionRatePercent }]
        }
      ]
    });
  }
}
