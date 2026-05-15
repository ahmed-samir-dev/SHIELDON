import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AttendanceService } from '../../../core/services/attendance.service';
import { AttendanceCheckDto } from '../../../core/models/attendance.model';

@Component({
  selector: 'app-attendance-admin',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './attendance-admin.html',
  styleUrls: ['./attendance-admin.scss']
})
export class AttendanceAdminComponent implements OnInit {
  private attendanceService = inject(AttendanceService);

  checks = signal<AttendanceCheckDto[]>([]);
  isLoading = signal<boolean>(true);
  errorMsg = signal<string>('');

  ngOnInit() {
    this.attendanceService.getAllChecksAdmin().subscribe({
      next: (res) => {
        this.checks.set(res.data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMsg.set('Failed to load attendance checks.');
        this.isLoading.set(false);
      }
    });
  }
}
