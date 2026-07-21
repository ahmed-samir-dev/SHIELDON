import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  LucideAngularModule, 
  Search, 
  ChevronLeft, 
  ChevronRight, 
  AlertTriangle, 
  CheckCircle, 
  ShieldAlert, 
  ShieldOff, 
  WifiOff, 
  History, 
  FileText, 
  User, 
  Clock,
  LayoutGrid
} from 'lucide-angular';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { IpAuditService, IpAuditLogDto, AuditTrailQueryParams } from '../../../core/services/ip-audit.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-audit-trail',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, TranslateModule],
  templateUrl: './audit-trail.html',
  styleUrl: './audit-trail.scss'
})
export class AuditTrailComponent implements OnInit, OnDestroy {
  private ipAuditService = inject(IpAuditService);
  private translate = inject(TranslateService);

  // Icons
  Search = Search;
  ChevronLeft = ChevronLeft;
  ChevronRight = ChevronRight;
  AlertTriangle = AlertTriangle;
  CheckCircle = CheckCircle;
  ShieldAlert = ShieldAlert;
  ShieldOff = ShieldOff;
  WifiOff = WifiOff;
  History = History;
  FileText = FileText;
  User = User;
  Clock = Clock;
  LayoutGrid = LayoutGrid;

  // State
  loading = signal<boolean>(true);
  error = signal<string>('');

  // Data
  logsList = signal<IpAuditLogDto[]>([]);
  totalCount = signal<number>(0);
  totalPages = signal<number>(0);
  currentPage = signal<number>(1);
  pageSize = 20;

  showingEnd = computed(() => Math.min(this.currentPage() * this.pageSize, this.totalCount()));

  // Filters
  searchQuery = signal<string>('');
  eventTypeFilter = signal<string>('');
  vpnFilter = signal<boolean | null>(null);
  duplicateFilter = signal<boolean | null>(null);
  netChangeFilter = signal<boolean | null>(null);

  // Search Debounce using RxJS
  private searchSubject = new Subject<string>();
  private searchSubscription!: Subscription;

  ngOnInit(): void {
    // Setup search debounce
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(query => {
      this.searchQuery.set(query);
      this.currentPage.set(1);
      this.loadAuditLogs();
    });

    this.loadAuditLogs();
  }

  ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  public loadAuditLogs(): void {
    this.loading.set(true);
    this.error.set('');

    const params: AuditTrailQueryParams = {
      page: this.currentPage(),
      pageSize: this.pageSize,
      eventType: this.eventTypeFilter() || undefined,
      isVpnOrProxy: this.vpnFilter() === null ? undefined : this.vpnFilter()!,
      isDuplicateSession: this.duplicateFilter() === null ? undefined : this.duplicateFilter()!,
      isNetworkChangeDuringExam: this.netChangeFilter() === null ? undefined : this.netChangeFilter()!
    };

    // Client-side filtering check for userName/IP in query if backend does not search it directly
    // Or we will send searches directly if required. Let's do it cleanly.
    this.ipAuditService.getAuditTrail(params).subscribe({
      next: (result) => {
        // If search query is present, do a client-side filter to be thorough,
        // or search directly if backend supports searching by username/IP.
        let filteredItems = result.items;
        const query = this.searchQuery().trim().toLowerCase();
        if (query) {
          filteredItems = filteredItems.filter(item => 
            item.userFullName.toLowerCase().includes(query) ||
            (item.ipAddress && item.ipAddress.toLowerCase().includes(query)) ||
            (item.userDisplayId && item.userDisplayId.toLowerCase().includes(query))
          );
        }

        this.logsList.set(filteredItems);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load audit logs', err);
        this.error.set(this.translate.instant('ADMIN_DASHBOARD.ERR_LOAD'));
        this.loading.set(false);
      }
    });
  }

  onSearchKeyup(value: string): void {
    this.searchSubject.next(value);
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadAuditLogs();
  }

  onPageChange(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadAuditLogs();
  }

  exportToCsv(): void {
    const headers = ['User', 'Display ID', 'Event Type', 'IP Address', 'VPN/Proxy', 'Duplicate Login', 'Network Change', 'Date'];
    const rows = this.logsList().map(log => [
      log.userFullName,
      log.userDisplayId || '',
      log.eventTypeLabel,
      log.ipAddress || '',
      log.isVpnOrProxy ? 'YES' : 'NO',
      log.isDuplicateSession ? 'YES' : 'NO',
      log.isNetworkChangeDuringExam ? 'YES' : 'NO',
      new Date(log.occurredAt).toLocaleString()
    ]);

    const csvContent = "data:text/csv;charset=utf-8," 
      + [headers.join(','), ...rows.map(e => e.map(val => `"${val.replace(/"/g, '""')}"`).join(','))].join('\n');
    
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `shieldon_ip_audit_trail_${Date.now()}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
