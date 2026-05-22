import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AttendanceService } from '../../../core/services/attendance.service';
import { AttendanceCheckDto } from '../../../core/models/attendance.model';
import { LanguageService } from '../../../core/services/language.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-attendance-admin',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './attendance-admin.html',
  styleUrls: ['./attendance-admin.scss']
})
export class AttendanceAdminComponent implements OnInit, OnDestroy {
  private attendanceService = inject(AttendanceService);
  private languageService = inject(LanguageService);
  public translate = inject(TranslateService);
  private langSub!: Subscription;

  checks = signal<AttendanceCheckDto[]>([]);
  isLoading = signal<boolean>(true);
  errorMsg = signal<string>('');

  ngOnInit() {
    this.loadChecks();
    this.langSub = this.languageService.languageChange$.subscribe(() => this.loadChecks());
  }

  ngOnDestroy() {
    this.langSub?.unsubscribe();
  }

  private loadChecks() {
    this.isLoading.set(true);
    this.attendanceService.getAllChecksAdmin().subscribe({
      next: (res) => {
        this.checks.set(res.data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMsg.set(this.translate.instant('ATTENDANCE_ADMIN.ERR_LOAD'));
        this.isLoading.set(false);
      }
    });
  }
}
