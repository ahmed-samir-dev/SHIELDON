import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AttendanceService } from '../../../core/services/attendance.service';
import { StudentAttendanceHistoryDto } from '../../../core/models/attendance.model';
import { Html5Qrcode, Html5QrcodeScannerState } from 'html5-qrcode';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-attendance-student',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './attendance-student.html',
  styleUrls: ['./attendance-student.scss']
})
export class AttendanceStudentComponent implements OnInit, OnDestroy {
  private attendanceService = inject(AttendanceService);

  isDev = !environment.production;
  history = signal<StudentAttendanceHistoryDto[]>([]);
  
  // Scanner state
  isScanning = signal<boolean>(false);
  scanResult = signal<{ success: boolean; message: string } | null>(null);
  private html5QrCode: Html5Qrcode | null = null;

  ngOnInit() {
    this.loadHistory();
  }

  ngOnDestroy() {
    this.stopScanner();
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
        this.scanResult.set({ success: false, message: `Failed to start camera: ${err}` });
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
      this.scanResult.set({ success: false, message: 'Invalid QR Code format.' });
      return;
    }

    const [checkId, secret] = parts;
    
    // Stop scanning immediately to prevent multiple rapid requests
    this.stopScanner();

    this.attendanceService.scanQrCode(checkId, { secret }).subscribe({
      next: (res) => {
        this.scanResult.set({ success: true, message: 'Attendance marked successfully!' });
        this.loadHistory(); // refresh history
      },
      error: (err) => {
        this.scanResult.set({ success: false, message: err.error?.message || 'Failed to mark attendance.' });
      }
    });
  }
}
