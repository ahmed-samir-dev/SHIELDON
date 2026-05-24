import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, Search, CheckCircle, XCircle, AlertCircle, Eye, RefreshCw, ChevronLeft, ChevronRight, Download } from 'lucide-angular';
import Swal from 'sweetalert2';
import { ReattemptService, ReattemptRequestResponse } from '../services/reattempt.service';
import { LanguageService } from '../../../core/services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-reattempt-requests',
  standalone: true,
  imports: [CommonModule, RouterModule, LucideAngularModule, TranslateModule],
  templateUrl: './reattempt-requests.html',
  styleUrl: './reattempt-requests.scss'
})
export class ReattemptRequestsComponent implements OnInit, OnDestroy {
  private reattemptService = inject(ReattemptService);
  private toastr = inject(ToastrService);
  private languageService = inject(LanguageService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;

  // Icons
  readonly Search = Search;
  readonly CheckCircle = CheckCircle;
  readonly XCircle = XCircle;
  readonly AlertCircle = AlertCircle;
  readonly Eye = Eye;
  readonly RefreshCw = RefreshCw;
  readonly ChevronLeft = ChevronLeft;
  readonly ChevronRight = ChevronRight;
  readonly Download = Download;

  // State
  isLoading = signal(true);
  requests = signal<ReattemptRequestResponse[]>([]);
  
  // Pagination & Filtering
  currentPage = signal(1);
  pageSize = signal(10);
  totalPages = signal(1);
  totalCount = signal(0);
  currentStatusFilter = signal<string>('All');
  searchTerm = signal<string>('');
  private searchTimeout: any;

  statusTabs = ['All', 'Pending', 'Approved', 'Rejected'];

  ngOnInit() {
    this.loadRequests();
    this.langSub = this.languageService.languageChange$.subscribe(() => this.loadRequests());
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
  }

  loadRequests() {
    this.isLoading.set(true);
    
    this.reattemptService.getRequests({
      page: this.currentPage(),
      pageSize: this.pageSize(),
      status: this.currentStatusFilter() === 'All' ? null : this.currentStatusFilter(),
      searchTerm: this.searchTerm() || null
    }).subscribe({
      next: (res) => {
        this.requests.set(res.data.items);
        this.totalPages.set(res.data.totalPages);
        this.totalCount.set(res.data.totalCount);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || this.translate.instant('REATTEMPT_REQUESTS.TOAST_ERR_LOAD'));
        this.isLoading.set(false);
      }
    });
  }

  filterByStatus(status: string) {
    this.currentStatusFilter.set(status);
    this.currentPage.set(1);
    this.loadRequests();
  }

  onSearch(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
    
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => {
      this.currentPage.set(1);
      this.loadRequests();
    }, 400);
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
      this.loadRequests();
    }
  }

  viewJustification(req: ReattemptRequestResponse) {
    Swal.fire({
      title: this.translate.instant('REATTEMPT_REQUESTS.SWAL_JUSTIFICATION_TITLE'),
      text: req.justification,
      icon: 'info',
      confirmButtonColor: '#215DAE'
    });
  }

  approveRequest(req: ReattemptRequestResponse) {
    Swal.fire({
      title: this.translate.instant('REATTEMPT_REQUESTS.SWAL_APPROVE_TITLE'),
        html: `
          <div class="swal-custom-reopen">
            <div class="info-banner">
              <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--theme-primary)" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>
              <span>${this.translate.instant('REATTEMPT_REQUESTS.SWAL_APPROVE_REOPEN_TEXT').replace('{student}', req.studentName)}</span>
            </div>
            <div class="extension-options">
              <label class="ext-btn" id="lbl-24">
                <input type="radio" name="extension" value="24" checked style="display: none;">
                <div>24h</div>
              </label>
              <label class="ext-btn" id="lbl-48">
                <input type="radio" name="extension" value="48" style="display: none;">
                <div>48h</div>
              </label>
              <label class="ext-btn" id="lbl-72">
                <input type="radio" name="extension" value="72" style="display: none;">
                <div>72h</div>
              </label>
            </div>
            <div class="custom-option">
              <label class="ext-btn" id="lbl-custom">
                <input type="radio" name="extension" value="custom" style="display: none;">
                <div>Custom Hours</div>
              </label>
              <div id="customInputWrapper">
                <input type="number" id="customHours" min="1" placeholder="Enter number of hours (e.g. 12)">
              </div>
            </div>
          </div>
        `,
        showCancelButton: true,
        confirmButtonColor: '#16A34A',
        cancelButtonColor: '#87949C',
        confirmButtonText: this.translate.instant('REATTEMPT_REQUESTS.BTN_APPROVE'),
        cancelButtonText: this.translate.instant('EXAM_RESULT_PAGE.SWAL_BTN_CANCEL'),
        background: 'var(--theme-bg-main)',
        color: 'var(--theme-text-main)',
        didOpen: () => {
          const radios = document.querySelectorAll('input[name="extension"]');
          const customWrapper = document.getElementById('customInputWrapper');
          
          const updateStyles = () => {
            radios.forEach((radio: any) => {
              const val = radio.value;
              const lbl = document.getElementById('lbl-' + val);
              if (lbl) {
                if (radio.checked) {
                  lbl.classList.add('selected');
                } else {
                  lbl.classList.remove('selected');
                }
              }
            });
          };

          // Initial style update
          updateStyles();

          radios.forEach(radio => {
            radio.addEventListener('change', (e: any) => {
              updateStyles();
              if (e.target.value === 'custom') {
                if (customWrapper) {
                  customWrapper.style.display = 'block';
                  setTimeout(() => document.getElementById('customHours')?.focus(), 50);
                }
              } else {
                if (customWrapper) customWrapper.style.display = 'none';
              }
            });
          });
        },
        preConfirm: () => {
          const selected = document.querySelector('input[name="extension"]:checked') as HTMLInputElement;
          if (!selected) {
            Swal.showValidationMessage('Please select an extension period');
            return false;
          }
          if (selected.value === 'custom') {
            const customHours = (document.getElementById('customHours') as HTMLInputElement).value;
            if (!customHours || parseInt(customHours) <= 0) {
              Swal.showValidationMessage('Please enter a valid number of hours > 0');
              return false;
            }
            return customHours;
          }
          return selected.value;
        }
      }).then((result) => {
        if (result.isConfirmed && result.value) {
          this.reattemptService.reviewRequest(req.id, { approved: true, extensionHours: parseInt(result.value as string) }).subscribe({
            next: (res) => {
              this.toastr.success(res.message);
              this.loadRequests();
            },
            error: (err) => this.toastr.error(err.error?.message || 'Approval failed')
          });
        }
      });
  }

  rejectRequest(id: string, studentName: string) {
    Swal.fire({
      title: this.translate.instant('REATTEMPT_REQUESTS.SWAL_REJECT_TITLE'),
      text: this.translate.instant('REATTEMPT_REQUESTS.SWAL_REJECT_DESC').replace('{student}', studentName),
      input: 'textarea',
      inputPlaceholder: this.translate.instant('REATTEMPT_REQUESTS.SWAL_REJECT_PLACEHOLDER'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#DC2626',
      cancelButtonColor: '#87949C',
      confirmButtonText: this.translate.instant('REATTEMPT_REQUESTS.SWAL_BTN_REJECT'),
      cancelButtonText: this.translate.instant('EXAM_RESULT_PAGE.SWAL_BTN_CANCEL')
    }).then((result) => {
      if (result.isConfirmed) {
        this.reattemptService.reviewRequest(id, { 
          approved: false, 
          rejectionReason: result.value || undefined 
        }).subscribe({
          next: (res) => {
            this.toastr.success(res.message);
            this.loadRequests();
          },
          error: (err) => this.toastr.error(err.error?.message || this.translate.instant('REATTEMPT_REQUESTS.TOAST_REJECT_ERR'))
        });
      }
    });
  }

  getFileUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (url.startsWith('http')) return url;
    
    // Fix double slashes by ensuring clean path
    const apiUrl = environment.apiUrl.replace('/api', '').replace(/\/$/, '');
    const cleanPath = url.startsWith('/') ? url : `/${url}`;
    
    return `${apiUrl}${cleanPath}`;
  }

  downloadFile(url: string | null | undefined, studentName: string) {
    if (!url) return;
    const fullUrl = this.getFileUrl(url);
    
    // Use fetch to get the file as a Blob, which forces download instead of preview
    // This works now because we enabled CORS for static files on the backend
    this.toastr.info(this.translate.instant('REATTEMPT_REQUESTS.TOAST_DOWNLOADING') || 'Downloading proof...');
    
    fetch(fullUrl)
      .then(response => {
        if (!response.ok) throw new Error('Network response was not ok');
        return response.blob();
      })
      .then(blob => {
        const blobUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = blobUrl;
        
        // Extract extension from url or fallback to jpg
        const ext = url.split('.').pop() || 'jpg';
        // Generate a clean filename: student-name-proof.jpg
        const cleanName = studentName.replace(/[^a-z0-9]/gi, '-').toLowerCase();
        a.download = `${cleanName}-proof.${ext}`;
        
        document.body.appendChild(a);
        a.click();
        
        // Cleanup
        window.URL.revokeObjectURL(blobUrl);
        document.body.removeChild(a);
      })
      .catch(err => {
        console.error('Download error:', err);
        this.toastr.error('Failed to download proof file. Please try again.');
      });
  }
}
