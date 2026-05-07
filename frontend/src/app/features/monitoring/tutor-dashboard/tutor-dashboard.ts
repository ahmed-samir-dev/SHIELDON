import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { LucideAngularModule, Monitor, Search, Filter, AlertTriangle, ShieldAlert, WifiOff, Users, Clock, ArrowRight, Eye } from 'lucide-angular';
import { NgxEchartsModule } from 'ngx-echarts';
import type { EChartsOption } from 'echarts';
import { Subscription, interval } from 'rxjs';
import { MonitoringService, TutorDashboardResponse, LiveSessionRow, ActiveExamSummary } from '../../../core/services/monitoring.service';

@Component({
  selector: 'app-tutor-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, LucideAngularModule, NgxEchartsModule],
  templateUrl: './tutor-dashboard.html',
  styleUrls: ['./tutor-dashboard.scss']
})
export class TutorDashboardComponent implements OnInit, OnDestroy {
  private monitoring = inject(MonitoringService);
  private router = inject(Router);

  // Icons
  Monitor = Monitor;
  Search = Search;
  Filter = Filter;
  AlertTriangle = AlertTriangle;
  ShieldAlert = ShieldAlert;
  WifiOff = WifiOff;
  Users = Users;
  Clock = Clock;
  ArrowRight = ArrowRight;
  Eye = Eye;

  // State
  loading = signal<boolean>(true);
  error = signal<string>('');
  dashboardData = signal<TutorDashboardResponse | null>(null);

  // Filtering
  searchQuery = signal<string>('');
  statusFilter = signal<string>('All'); // All | InProgress | Disconnected | Submitted | ForceSubmitted
  
  // Charts
  violationChartOptions = signal<EChartsOption>({});

  private pollSub?: Subscription;

  ngOnInit() {
    this.loadDashboard(true);
    // Poll every 10 seconds
    this.pollSub = interval(10000).subscribe(() => {
      this.loadDashboard(false);
    });
  }

  ngOnDestroy() {
    this.pollSub?.unsubscribe();
  }

  loadDashboard(showLoading: boolean) {
    if (showLoading) this.loading.set(true);
    
    this.monitoring.getTutorDashboard().subscribe({
      next: (res) => {
        this.dashboardData.set(res.data || null);
        if (res.data) {
          this.initCharts(res.data);
        }
        if (showLoading) this.loading.set(false);
      },
      error: () => {
        if (showLoading) {
          this.error.set('Failed to load dashboard data.');
          this.loading.set(false);
        }
      }
    });
  }

  get filteredSessions(): LiveSessionRow[] {
    const data = this.dashboardData();
    if (!data || !data.liveSessions) return [];

    let filtered = data.liveSessions;
    const query = this.searchQuery().toLowerCase();
    const status = this.statusFilter();

    if (query) {
      filtered = filtered.filter(s => 
        s.studentName.toLowerCase().includes(query) || 
        s.studentCode.toLowerCase().includes(query) ||
        s.examTitle.toLowerCase().includes(query)
      );
    }

    if (status !== 'All') {
      filtered = filtered.filter(s => s.status === status);
    }

    return filtered;
  }

  get highRiskSessions(): LiveSessionRow[] {
    const data = this.dashboardData();
    if (!data || !data.liveSessions) return [];
    return data.liveSessions.filter(s => s.status === 'InProgress' && s.violationCount >= 2);
  }

  private initCharts(data: TutorDashboardResponse) {
    // Doughnut Chart: Violation Distribution
    if (data.violationDistribution && data.violationDistribution.items.length > 0) {
      const chartData = data.violationDistribution.items.map(item => ({
        name: item.violationType,
        value: item.count
      }));

      this.violationChartOptions.set({
        tooltip: { trigger: 'item' },
        legend: { bottom: '0%', left: 'center', icon: 'circle', textStyle: { fontSize: 12 } },
        series: [
          {
            name: 'Violations',
            type: 'pie',
            radius: ['40%', '70%'],
            avoidLabelOverlap: false,
            itemStyle: {
              borderRadius: 10,
              borderColor: '#fff',
              borderWidth: 2
            },
            label: { show: false, position: 'center' },
            emphasis: {
              label: { show: true, fontSize: 16, fontWeight: 'bold' }
            },
            labelLine: { show: false },
            data: chartData
          }
        ]
      });
    }
  }

  getStatusDisplay(status: string) {
    switch (status) {
      case 'InProgress': return { label: 'Active', icon: 'monitor', classes: 'bg-blue-100 text-blue-700' };
      case 'Disconnected': return { label: 'Disconnected', icon: 'wifi-off', classes: 'bg-orange-100 text-orange-700' };
      case 'Submitted': return { label: 'Submitted', icon: 'check-circle', classes: 'bg-green-100 text-green-700' };
      case 'ForceSubmitted': return { label: 'Terminated', icon: 'shield-alert', classes: 'bg-red-100 text-red-700' };
      default: return { label: status, icon: 'info', classes: 'bg-slate-100 text-slate-700' };
    }
  }

  goToReview(attemptId: string) {
    this.router.navigate(['/exam-attempts', attemptId, 'manual-review']);
  }
}
