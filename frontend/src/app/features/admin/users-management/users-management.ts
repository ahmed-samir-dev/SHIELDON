import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Users, Search, LockKeyhole, LockKeyholeOpen, ChevronLeft, ChevronRight, AlertTriangle, CheckCircle, Clock, XCircle } from 'lucide-angular';
import Swal from 'sweetalert2';
import { Subject, Subscription, firstValueFrom } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { UserService } from '../../../core/services/user.service';
import { UserDetailDto, UserFilterParams, AccountStatus } from '../../../core/models/user.model';
import { PagedResponse } from '../../../core/models/api-response.model';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-users-management',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './users-management.html',
  styleUrl: './users-management.scss'
})
export class UsersManagementComponent implements OnInit, OnDestroy {
  private userService = inject(UserService);

  // Icons
  Users = Users;
  Search = Search;
  LockKeyhole = LockKeyhole;
  LockKeyholeOpen = LockKeyholeOpen;
  ChevronLeft = ChevronLeft;
  ChevronRight = ChevronRight;
  AlertTriangle = AlertTriangle;
  CheckCircle = CheckCircle;
  Clock = Clock;
  XCircle = XCircle;

  // State — prefixed with "users" to avoid conflicts with parent dashboard
  usersLoading = signal(true);
  usersError = signal('');

  // Data
  usersList = signal<UserDetailDto[]>([]);
  usersTotalCount = signal(0);
  usersTotalPages = signal(0);
  usersCurrentPage = signal(1);

  // Aliases for template binding
  loading = this.usersLoading;
  totalCount = this.usersTotalCount;

  // Filters
  searchQuery = '';
  roleFilter = '';
  statusFilter = '';
  readonly usersPageSize = 10;

  // Search Debounce using RxJS
  private searchSubject = new Subject<string>();
  private searchSubscription!: Subscription;

  get apiBase(): string { return environment.apiUrl.replace('/api', ''); }

  getAvatarUrl(user: UserDetailDto): string | null {
    if (!user.profilePictureUrl) return null;
    if (user.profilePictureUrl.startsWith('http')) return user.profilePictureUrl;
    return `${this.apiBase}/${user.profilePictureUrl}`;
  }

  getInitials(user: UserDetailDto): string {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }

  getUsersPagesArray(): number[] {
    const total = this.usersTotalPages();
    const current = this.usersCurrentPage();
    const pages: number[] = [];
    const delta = 2;
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      pages.push(i);
    }
    return pages;
  }

  getUsersPageEnd(): number {
    return Math.min(this.usersCurrentPage() * this.usersPageSize, this.usersTotalCount());
  }

  ngOnInit(): void {
    // Setup RxJS debounce for search
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(() => {
      this.usersCurrentPage.set(1);
      this.loadUsers();
    });

    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  loadUsers(): void {
    this.usersLoading.set(true);
    this.usersError.set('');

    const filters: UserFilterParams = {
      page: this.usersCurrentPage(),
      pageSize: this.usersPageSize,
      search: this.searchQuery || undefined,
      role: this.roleFilter || undefined,
      status: this.statusFilter || undefined,
    };

    this.userService.getUsers(filters).subscribe({
      next: (res: PagedResponse<UserDetailDto>) => {
        this.usersList.set(res.items as UserDetailDto[]);
        this.usersTotalCount.set(res.totalCount);
        this.usersTotalPages.set(res.totalPages);
        this.usersLoading.set(false);
      },
      error: () => {
        this.usersError.set('Failed to load users. Please try again.');
        this.usersLoading.set(false);
      }
    });
  }

  onSearchChange(): void {
    this.searchSubject.next(this.searchQuery);
  }

  onFilterChange(): void {
    this.usersCurrentPage.set(1);
    this.loadUsers();
  }

  changeUsersPage(page: number): void {
    if (page >= 1 && page <= this.usersTotalPages()) {
      this.usersCurrentPage.set(page);
      this.loadUsers();
    }
  }

  async lockUser(user: UserDetailDto): Promise<void> {
    const result = await Swal.fire({
      title: 'Lock Account?',
      html: `This will prevent <strong>${user.fullName}</strong> from logging in until unlocked.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Yes, Lock Account',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#dc2626',
      customClass: { popup: 'swal-shieldon' },
      showLoaderOnConfirm: true,
      preConfirm: async () => {
        try {
          await firstValueFrom(this.userService.lockUser(user.id));
          return true;
        } catch (error: any) {
          Swal.showValidationMessage(error.error?.message || 'Could not lock the account.');
          return false;
        }
      },
      allowOutsideClick: () => !Swal.isLoading()
    });

    if (result.isConfirmed && result.value) {
      this.usersList.update(list => list.map(u =>
        u.id === user.id ? { ...u, accountStatus: 'Locked' as AccountStatus, lockedAt: new Date().toISOString() } : u
      ));
      Swal.fire({ title: 'Locked', text: `${user.fullName}'s account has been locked.`, icon: 'success', timer: 2000, showConfirmButton: false });
    }
  }

  async unlockUser(user: UserDetailDto): Promise<void> {
    const result = await Swal.fire({
      title: 'Unlock Account?',
      html: `This will restore access for <strong>${user.fullName}</strong> and reset failed login attempts.`,
      icon: 'question',
      showCancelButton: true,
      confirmButtonText: 'Yes, Unlock',
      cancelButtonText: 'Cancel',
      confirmButtonColor: '#16a34a',
      customClass: { popup: 'swal-shieldon' },
      showLoaderOnConfirm: true,
      preConfirm: async () => {
        try {
          await firstValueFrom(this.userService.unlockUser(user.id));
          return true;
        } catch (error: any) {
          Swal.showValidationMessage(error.error?.message || 'Could not unlock the account.');
          return false;
        }
      },
      allowOutsideClick: () => !Swal.isLoading()
    });

    if (result.isConfirmed && result.value) {
      this.usersList.update(list => list.map(u =>
        u.id === user.id ? { ...u, accountStatus: 'Active' as AccountStatus, lockedAt: null, failedLoginAttempts: 0 } : u
      ));
      Swal.fire({ title: 'Unlocked', text: `${user.fullName}'s account has been restored.`, icon: 'success', timer: 2000, showConfirmButton: false });
    }
  }


  getRoleId(user: UserDetailDto): string {
    return user.studentId || user.tutorId || '—';
  }

  trackByUserId(index: number, user: UserDetailDto): string {
    return user.id;
  }
}
