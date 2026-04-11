import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, RegisterRequest } from '../models/auth.model';
import { UserRole } from '../models/user-role.enum';

const ACCESS_TOKEN_KEY = 'shieldon_access_token';
const REFRESH_TOKEN_KEY = 'shieldon_refresh_token';
const USER_KEY = 'shieldon_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  // ── Reactive State (Angular Signals) ──────────────────────────────────────
  private _currentUser = signal<LoginResponse | null>(this._loadFromStorage());
  
  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this._currentUser() !== null);
  readonly userRole = computed(() => this._currentUser()?.role ?? null);
  readonly isAdmin = computed(() => this._currentUser()?.role === UserRole.Admin);
  readonly isTutor = computed(() => this._currentUser()?.role === UserRole.Tutor);
  readonly isStudent = computed(() => this._currentUser()?.role === UserRole.Student);

  // ── Public API ────────────────────────────────────────────────────────────

  register(request: RegisterRequest): Observable<any> {
    return this.http.post(`${environment.apiUrl}/auth/register`, request);
  }

  login(request: LoginRequest): Observable<{ data: LoginResponse }> {
    return this.http.post<{ data: LoginResponse }>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap(response => {
        this._persist(response.data);
      })
    );
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      // Fire-and-forget: revoke on server, don't wait for response
      this.http.post(`${environment.apiUrl}/auth/logout`, { refreshToken }).subscribe({
        error: () => {} // Silently ignore network errors on logout
      });
    }
    this._clearStorage();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  refreshAccessToken(): Observable<{ data: LoginResponse }> {
    const refreshToken = this.getRefreshToken();
    return this.http.post<{ data: LoginResponse }>(
      `${environment.apiUrl}/auth/refresh`,
      { refreshToken }
    ).pipe(
      tap(response => {
        this._persist(response.data);
      })
    );
  }

  verifyEmail(request: { email: string, token: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/auth/verify-email`, request);
  }

  resendVerification(request: { email: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/auth/resend-verification`, request);
  }

  forgotPassword(request: { email: string }): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/auth/forgot-password`, request);
  }

  resetPassword(request: any): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${environment.apiUrl}/auth/reset-password`, request);
  }

  updateUserIdentity(update: Partial<LoginResponse>): void {
    const current = this._currentUser();
    if (current) {
      const updated = { ...current, ...update };
      // Update cache
      localStorage.setItem(USER_KEY, JSON.stringify(updated));
      // Update signal state
      this._currentUser.set(updated);
    }
  }

  // ── Storage Helpers ───────────────────────────────────────────────────────

  private _persist(user: LoginResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, user.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, user.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._currentUser.set(user);
  }

  private _clearStorage(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._currentUser.set(null);
  }

  private _loadFromStorage(): LoginResponse | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
}
