import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, ShieldAlert, AlertTriangle, AlertCircle, FileText, CheckCircle, Activity, Info } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, ViolationSummaryResponse } from '../../../core/services/monitoring.service';

@Component({
  selector: 'app-violation-timeline',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, NgxEchartsModule],
  templateUrl: './violation-timeline.html',
  styleUrls: ['./violation-timeline.scss']
})
export class ViolationTimelineComponent implements OnInit {
  @Input() attemptId!: string;

  private monitoring = inject(MonitoringService);

  // Icons
  ShieldAlert = ShieldAlert;
  AlertTriangle = AlertTriangle;
  AlertCircle = AlertCircle;
  FileText = FileText;
  CheckCircle = CheckCircle;
  Activity = Activity;
  Info = Info;

  // State
  loading = signal<boolean>(true);
  error = signal<string>('');
  summary = signal<ViolationSummaryResponse | null>(null);
  
  // ECharts
  chartOptions = signal<EChartsOption>({});

  ngOnInit() {
    this.loadViolations();
  }

  loadViolations() {
    if (!this.attemptId) return;

    this.loading.set(true);
    this.monitoring.getViolationSummary(this.attemptId).subscribe({
      next: (res) => {
        this.summary.set(res.data || null);
        if (res.data) this.initChart(res.data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load violation summary.');
        this.loading.set(false);
      }
    });
  }

  private initChart(data: ViolationSummaryResponse) {
    if (!data.chartData || data.chartData.length === 0) return;

    const xAxisData = data.chartData.map(d => `Min ${d.minuteOffset}`);
    const criticalData = data.chartData.map(d => d.criticalCount);
    const mediumData = data.chartData.map(d => d.mediumCount);
    const minorData = data.chartData.map(d => d.minorCount);

    this.chartOptions.set({
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'shadow' }
      },
      legend: {
        data: ['Critical', 'Medium', 'Minor'],
        bottom: 0,
        icon: 'circle'
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '15%',
        top: '5%',
        containLabel: true
      },
      xAxis: {
        type: 'category',
        data: xAxisData,
        axisLine: { lineStyle: { color: '#cbd5e1' } },
        axisLabel: { color: '#64748b' }
      },
      yAxis: {
        type: 'value',
        minInterval: 1,
        axisLine: { show: false },
        axisTick: { show: false },
        splitLine: { lineStyle: { color: '#f1f5f9', type: 'dashed' } },
        axisLabel: { color: '#64748b' }
      },
      series: [
        {
          name: 'Critical',
          type: 'bar',
          stack: 'total',
          data: criticalData,
          itemStyle: { color: '#ef4444' }, // red-500
          barMaxWidth: 40
        },
        {
          name: 'Medium',
          type: 'bar',
          stack: 'total',
          data: mediumData,
          itemStyle: { color: '#f97316' }, // orange-500
          barMaxWidth: 40
        },
        {
          name: 'Minor',
          type: 'bar',
          stack: 'total',
          data: minorData,
          itemStyle: { color: '#eab308' }, // yellow-500
          barMaxWidth: 40
        }
      ]
    });
  }

  getSeverityIcon(severity: string): string {
    switch (severity) {
      case 'Critical': return 'ShieldAlert';
      case 'Medium': return 'AlertTriangle';
      case 'Minor': return 'AlertCircle';
      default: return 'Info';
    }
  }

  getSeverityColor(severity: string) {
    switch (severity) {
      case 'Critical': return 'text-red-600 bg-red-50 ring-red-500/20';
      case 'Medium': return 'text-orange-500 bg-orange-50 ring-orange-500/20';
      case 'Minor': return 'text-yellow-600 bg-yellow-50 ring-yellow-500/20';
      default: return 'text-blue-500 bg-blue-50 ring-blue-500/20';
    }
  }

  getSubmissionTypeDisplay(type: string) {
    switch(type) {
      case 'ForceSubmitted': return { label: 'Force Submitted', classes: 'bg-red-100 text-red-700 border-red-200' };
      case 'AutoExpired': return { label: 'Auto Expired', classes: 'bg-slate-100 text-slate-700 border-slate-200' };
      case 'Manual': return { label: 'Manually Submitted', classes: 'bg-green-100 text-green-700 border-green-200' };
      default: return { label: 'In Progress', classes: 'bg-blue-100 text-blue-700 border-blue-200' };
    }
  }
}
