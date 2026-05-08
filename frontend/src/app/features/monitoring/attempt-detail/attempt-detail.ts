import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { LucideAngularModule, AlertTriangle, CheckCircle, Clock, ShieldAlert, Monitor, User, ArrowLeft, Calendar, FileText, Activity, AlertCircle } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, AttemptTimelineResponse, ViolationSummaryResponse } from '../../../core/services/monitoring.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-attempt-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, NgxEchartsModule],
  templateUrl: './attempt-detail.html',
  styleUrls: ['./attempt-detail.scss']
})
export class AttemptDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private monitoring = inject(MonitoringService);
  apiUrl = environment.apiUrl.replace('/api', '');

  // Icons
  AlertTriangle = AlertTriangle;
  CheckCircle = CheckCircle;
  Clock = Clock;
  ShieldAlert = ShieldAlert;
  Monitor = Monitor;
  User = User;
  ArrowLeft = ArrowLeft;
  Calendar = Calendar;
  FileText = FileText;
  Activity = Activity;
  AlertCircle = AlertCircle;

  loading = signal<boolean>(true);
  error = signal<string>('');
  
  timelineData = signal<AttemptTimelineResponse | null>(null);
  summaryData = signal<ViolationSummaryResponse | null>(null);

  chartOptions = signal<EChartsOption>({});

  ngOnInit() {
    const attemptId = this.route.snapshot.paramMap.get('attemptId');
    if (!attemptId) {
      this.error.set('No attempt ID provided.');
      this.loading.set(false);
      return;
    }

    this.loadData(attemptId);
  }

  private loadData(attemptId: string) {
    this.loading.set(true);
    
    // We could use forkJoin but sequentially or parallel standard is fine. We will do it simple.
    let timelineLoaded = false;
    let summaryLoaded = false;
    let hasError = false;

    const checkDone = () => {
      if (timelineLoaded && summaryLoaded) {
        this.loading.set(false);
        this.initChart();
      }
    };

    this.monitoring.getAttemptTimeline(attemptId).subscribe({
      next: (res) => {
        if (res.data) this.timelineData.set(res.data);
        timelineLoaded = true;
        checkDone();
      },
      error: () => {
        if (!hasError) {
          hasError = true;
          this.error.set('Failed to load attempt timeline.');
          this.loading.set(false);
        }
      }
    });

    this.monitoring.getViolationSummary(attemptId).subscribe({
      next: (res) => {
        if (res.data) this.summaryData.set(res.data);
        summaryLoaded = true;
        checkDone();
      },
      error: () => {
        if (!hasError) {
          hasError = true;
          this.error.set('Failed to load violation summary.');
          this.loading.set(false);
        }
      }
    });
  }

  private initChart() {
    const summary = this.summaryData();
    if (!summary || !summary.chartData || summary.chartData.length === 0) return;

    const minutes = summary.chartData.map(d => `Min ${d.minuteOffset}`);
    const critical = summary.chartData.map(d => d.criticalCount);
    const medium = summary.chartData.map(d => d.mediumCount);
    const minor = summary.chartData.map(d => d.minorCount);

    this.chartOptions.set({
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'shadow' }
      },
      legend: {
        data: ['Critical', 'Medium', 'Minor'],
        bottom: 0
      },
      grid: {
        left: '3%',
        right: '4%',
        bottom: '15%',
        top: '10%',
        containLabel: true
      },
      xAxis: {
        type: 'category',
        data: minutes,
        axisLine: { lineStyle: { color: '#cbd5e1' } }
      },
      yAxis: {
        type: 'value',
        minInterval: 1,
        axisLine: { show: false },
        splitLine: { lineStyle: { color: '#f1f5f9', type: 'dashed' } }
      },
      series: [
        {
          name: 'Critical',
          type: 'bar',
          stack: 'total',
          data: critical,
          itemStyle: { color: '#ef4444' } // red-500
        },
        {
          name: 'Medium',
          type: 'bar',
          stack: 'total',
          data: medium,
          itemStyle: { color: '#f97316' } // orange-500
        },
        {
          name: 'Minor',
          type: 'bar',
          stack: 'total',
          data: minor,
          itemStyle: { color: '#3b82f6' } // blue-500
        }
      ]
    });
  }

  getSeverityBadge(severity: string) {
    switch (severity) {
      case 'Critical': return 'bg-red-100 text-red-700 border-red-200';
      case 'Medium': return 'bg-amber-100 text-amber-700 border-amber-200';
      case 'Minor': return 'bg-blue-100 text-blue-700 border-blue-200';
      default: return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }

  getSeverityBadgeClass(severity: string) {
    switch (severity) {
      case 'Critical': return 'sev-critical';
      case 'Medium': return 'sev-medium';
      case 'Minor': return 'sev-minor';
      default: return 'sev-minor';
    }
  }

  getCircleClass(type: string, severity?: string) {
    if (type === 'Violation') {
      switch (severity) {
        case 'Critical': return 'circle-critical';
        case 'Medium': return 'circle-medium';
        default: return 'circle-minor';
      }
    }
    switch (type) {
      case 'ExamStarted': return 'circle-system';
      case 'ExamSubmitted': return 'circle-done';
      case 'ExamTerminated': return 'circle-critical';
      default: return 'circle-system';
    }
  }

  getTimelineIcon(type: string) {
    switch (type) {
      case 'ExamStarted': return 'Play';
      case 'ExamSubmitted': return 'CheckCircle';
      case 'ExamTerminated': return 'ShieldAlert';
      case 'Violation': return 'AlertTriangle';
      default: return 'Activity';
    }
  }

  getTimelineIconColor(type: string, severity?: string) {
    if (type === 'Violation') {
      switch (severity) {
        case 'Critical': return 'text-red-600 bg-red-100';
        case 'Medium': return 'text-amber-600 bg-amber-100';
        case 'Minor': return 'text-blue-600 bg-blue-100';
        default: return 'text-slate-600 bg-slate-100';
      }
    }
    
    switch (type) {
      case 'ExamStarted': return 'text-indigo-600 bg-indigo-100';
      case 'ExamSubmitted': return 'text-emerald-600 bg-emerald-100';
      case 'ExamTerminated': return 'text-red-600 bg-red-100';
      default: return 'text-slate-600 bg-slate-100';
    }
  }

  goBack() {
    window.history.back();
  }
}
