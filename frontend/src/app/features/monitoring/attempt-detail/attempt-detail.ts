import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { LucideAngularModule, AlertTriangle, CheckCircle, Clock, ShieldAlert, Monitor, User, ArrowLeft, Calendar, FileText, Activity, AlertCircle, Play } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { MonitoringService, AttemptTimelineResponse, ViolationSummaryResponse } from '../../../core/services/monitoring.service';
import { ThemeService } from '../../../core/services/theme.service';
import { environment } from '../../../../environments/environment';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-attempt-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, NgxEchartsModule, TranslateModule],
  templateUrl: './attempt-detail.html',
  styleUrl: './attempt-detail.scss'
})
export class AttemptDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private monitoring = inject(MonitoringService);
  private themeService = inject(ThemeService);
  public translate = inject(TranslateService);
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
  Play = Play;

  Math = Math;

  loading = signal<boolean>(true);
  error = signal<string>('');
  
  timelineData = signal<AttemptTimelineResponse | null>(null);
  summaryData = signal<ViolationSummaryResponse | null>(null);

  violationScore = computed(() => {
    const data = this.timelineData();
    if (!data) return 0;
    return (data.criticalCount * 1.0) + (data.mediumCount * 1.0) + (data.minorCount * 0.5);
  });

  chartOptions = computed<EChartsOption>(() => {
    const summary = this.summaryData();
    const activeTheme = this.themeService.activeTheme();
    
    if (!summary || !summary.chartData || summary.chartData.length === 0) {
      return {};
    }

    const minutes = summary.chartData.map(d => `Min ${d.minuteOffset}`);
    const criticalData = summary.chartData.map((d, i) => d.criticalCount > 0 ? [i, 2, d.criticalCount] : null).filter(x => x !== null);
    const mediumData = summary.chartData.map((d, i) => d.mediumCount > 0 ? [i, 1, d.mediumCount] : null).filter(x => x !== null);
    const minorData = summary.chartData.map((d, i) => d.minorCount > 0 ? [i, 0, d.minorCount] : null).filter(x => x !== null);

    const isDark = activeTheme === 'dark';
    const textColor = isDark ? '#94a3b8' : '#64748b';
    const lineColor = isDark ? '#334155' : '#cbd5e1';
    const splitLineColor = isDark ? '#1e293b' : '#f1f5f9';

    return {
      tooltip: {
        trigger: 'item',
        formatter: (params: any) => {
          const min = minutes[params.value[0]];
          const type = params.value[1] === 2 ? 'Critical' : params.value[1] === 1 ? 'Medium' : 'Minor';
          return `${min}<br/>${type}: <b>${params.value[2]}</b>`;
        }
      },
      legend: {
        data: [
          this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_CRITICAL'),
          this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_MEDIUM'),
          this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_MINOR')
        ],
        bottom: 0,
        textStyle: { color: textColor }
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
        data: minutes,
        axisLine: { lineStyle: { color: lineColor } },
        axisLabel: { color: textColor },
        splitLine: { show: true, lineStyle: { color: splitLineColor, type: 'dashed' } }
      },
      yAxis: {
        type: 'category',
        data: [
          this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_MINOR'),
          this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_MEDIUM'),
          this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_CRITICAL')
        ],
        axisLine: { show: false },
        axisLabel: { color: textColor, fontWeight: 'bold' },
        splitLine: { show: true, lineStyle: { color: splitLineColor, type: 'dashed' } }
      },
      series: [
        {
          name: this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_CRITICAL'),
          type: 'scatter',
          data: criticalData,
          symbolSize: (val: any) => Math.min(Math.max(val[2] * 12, 16), 40),
          itemStyle: { color: '#ef4444', opacity: 0.8 }
        },
        {
          name: this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_MEDIUM'),
          type: 'scatter',
          data: mediumData,
          symbolSize: (val: any) => Math.min(Math.max(val[2] * 12, 16), 40),
          itemStyle: { color: '#f97316', opacity: 0.8 }
        },
        {
          name: this.translate.instant('ATTEMPT_DETAIL.CHART_LEGEND_MINOR'),
          type: 'scatter',
          data: minorData,
          symbolSize: (val: any) => Math.min(Math.max(val[2] * 12, 16), 40),
          itemStyle: { color: '#3b82f6', opacity: 0.8 }
        }
      ]
    };
  });

  ngOnInit() {
    const attemptId = this.route.snapshot.paramMap.get('attemptId');
    if (!attemptId) {
      this.error.set(this.translate.instant('ATTEMPT_DETAIL.ERR_NO_ID'));
      this.loading.set(false);
      return;
    }

    this.loadData(attemptId);
  }

  private loadData(attemptId: string) {
    this.loading.set(true);
    
    let timelineLoaded = false;
    let summaryLoaded = false;
    let hasError = false;

    const checkDone = () => {
      if (timelineLoaded && summaryLoaded) {
        this.loading.set(false);
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
          this.error.set(this.translate.instant('ATTEMPT_DETAIL.ERR_LOAD_TIMELINE'));
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
          this.error.set(this.translate.instant('ATTEMPT_DETAIL.ERR_LOAD_SUMMARY'));
          this.loading.set(false);
        }
      }
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

  getCircleClass(category: string, eventType: string, severity: string) {
    if (category === 'Violation') {
      switch (severity) {
        case 'Critical': return 'circle-critical';
        case 'Medium': return 'circle-medium';
        default: return 'circle-minor';
      }
    }
    if (category === 'Presence') {
      switch (eventType) {
        case 'Disconnected': return 'circle-critical';
        case 'Reconnected': return 'circle-done';
        case 'PageRefreshed': return 'circle-minor';
        default: return 'circle-system';
      }
    }
    switch (eventType) {
      case 'ExamStarted': return 'circle-system';
      case 'ExamSubmitted': return 'circle-done';
      case 'ExamTerminated': return 'circle-critical';
      default: return 'circle-system';
    }
  }

  getTimelineIcon(category: string, eventType: string) {
    if (category === 'Violation') return this.AlertTriangle;
    if (category === 'Presence') {
      switch (eventType) {
        case 'Disconnected': return this.AlertCircle;
        case 'Reconnected': return this.Activity;
        case 'PageRefreshed': return this.Activity;
        default: return this.Activity;
      }
    }
    switch (eventType) {
      case 'ExamStarted': return this.Play;
      case 'ExamSubmitted': return this.CheckCircle;
      case 'ExamTerminated': return this.ShieldAlert;
      default: return this.Activity;
    }
  }

  getTimelineIconColor(category: string, eventType: string, severity: string) {
    if (category === 'Violation') {
      switch (severity) {
        case 'Critical': return 'text-red-600 bg-red-100';
        case 'Medium': return 'text-amber-600 bg-amber-100';
        case 'Minor': return 'text-blue-600 bg-blue-100';
        default: return 'text-slate-600 bg-slate-100';
      }
    }
    if (category === 'Presence') {
      switch (eventType) {
        case 'Disconnected': return 'text-red-600 bg-red-100';
        case 'Reconnected': return 'text-emerald-600 bg-emerald-100';
        case 'PageRefreshed': return 'text-blue-600 bg-blue-100';
        default: return 'text-slate-600 bg-slate-100';
      }
    }
    switch (eventType) {
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
