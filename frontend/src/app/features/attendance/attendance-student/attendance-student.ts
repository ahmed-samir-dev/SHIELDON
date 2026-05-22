import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AttendanceService } from '../../../core/services/attendance.service';
import { StudentAttendanceHistoryDto } from '../../../core/models/attendance.model';
import { Html5Qrcode, Html5QrcodeScannerState } from 'html5-qrcode';
import { environment } from '../../../../environments/environment';
import { LanguageService } from '../../../core/services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-attendance-student',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './attendance-student.html',
  styleUrls: ['./attendance-student.scss']
})
export class AttendanceStudentComponent implements OnInit, OnDestroy {
  private attendanceService = inject(AttendanceService);
  private languageService = inject(LanguageService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;

  isDev = !environment.production;
  history = signal<StudentAttendanceHistoryDto[]>([]);
  
  // Scanner state
  isScanning = signal<boolean>(false);
  scanResult = signal<{ success: boolean; message: string } | null>(null);
  private html5QrCode: Html5Qrcode | null = null;

  ngOnInit() {
    this.loadHistory();
    this.langSub = this.languageService.languageChange$.subscribe(() => this.loadHistory());
  }

  ngOnDestroy() {
    this.stopScanner();
    this.langSub?.unsubscribe();
  }

  private loadHistory() {
    this.attendanceService.getStudentHistory().subscribe({
      next: (res) => this.history.set(res.data),
      error: (err) => console.error('Failed to load history', err)
    });
  }

  startScanner() {
    this.isScanning.set(true);
    this.scanResult.set(null);
    
    // We need a slight delay for the DOM to render the #reader div
    setTimeout(() => {
      this.html5QrCode = new Html5Qrcode("reader");
      
      this.html5QrCode.start(
        { facingMode: "environment" },
        { fps: 10, qrbox: { width: 250, height: 250 } },
        (decodedText, decodedResult) => {
          this.handleSuccessfulScan(decodedText);
        },
        (errorMessage) => {
          // parse errors are ignored, it just keeps scanning
        }
      ).catch(err => {
        this.scanResult.set({ success: false, message: this.translate.instant('ATTENDANCE_STUDENT.ERR_CAMERA').replace('{err}', err) });
        this.isScanning.set(false);
      });
    }, 100);
  }

  stopScanner() {
    if (this.html5QrCode && this.html5QrCode.getState() !== Html5QrcodeScannerState.NOT_STARTED) {
      this.html5QrCode.stop().then(() => {
        this.html5QrCode?.clear();
        this.isScanning.set(false);
      }).catch(err => console.error('Failed to stop scanner', err));
    } else {
      this.isScanning.set(false);
    }
  }

  // Changed to public so the template can call it
  handleSuccessfulScan(decodedText: string) {
    // Expected format: "{checkId}|{secret}"
    const parts = decodedText.split('|');
    if (parts.length !== 2) {
      this.stopScanner();
      this.scanResult.set({ success: false, message: this.translate.instant('ATTENDANCE_STUDENT.ERR_INVALID_QR') });
      return;
    }

    const [checkId, secret] = parts;
    
    // Stop scanning immediately to prevent multiple rapid requests
    this.stopScanner();

    this.attendanceService.scanQrCode(checkId, { secret }).subscribe({
      next: (res) => {
        this.scanResult.set({ success: true, message: this.translate.instant('ATTENDANCE_STUDENT.MSG_SUCCESS') });
        this.loadHistory(); // refresh history
      },
      error: (err) => {
        this.scanResult.set({ success: false, message: err.error?.message || this.translate.instant('ATTENDANCE_STUDENT.MSG_FAILED') });
      }
    });
  }
}
