import { Injectable, inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError, switchMap, BehaviorSubject, filter, take, Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AuthInterceptorService {
  private authService = inject(AuthService);
  private router = inject(Router);

  private isRefreshing = false;
  private refreshSubject = new BehaviorSubject<string | null>(null);

  public intercept(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<any> {
    // Skip auth endpoints to avoid infinite loops & handling 401s on auth routes
    if (this.isAuthRoute(req.url)) {
      return next(req);
    }

    const token = this.authService.getAccessToken();
    const authReq = token ? this.addToken(req, token) : req;

    return next(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && this.authService.getRefreshToken()) {
          return this.handle401Error(req, next);
        }
        return throwError(() => error);
      })
    );
  }

  public resetState(): void {
    this.isRefreshing = false;
    this.refreshSubject.next(null);
  }

  private handle401Error(req: HttpRequest<unknown>, next: HttpHandlerFn): Observable<any> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshSubject.next(null);

      return this.authService.refreshAccessToken().pipe(
        switchMap(response => {
          this.isRefreshing = false;
          const newToken = response.data.accessToken;
          this.refreshSubject.next(newToken);
          return next(this.addToken(req, newToken));
        }),
        catchError(err => {
          this.isRefreshing = false;
          this.authService.logout();
          this.router.navigate(['/login']);
          return throwError(() => err);
        })
      );
    }

    // If already refreshing, queue subsequent requests to wait for the new token
    return this.refreshSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap(token => next(this.addToken(req, token!)))
    );
  }

  private addToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  private isAuthRoute(url: string): boolean {
    return (
      url.includes('/auth/login') ||
      url.includes('/auth/refresh') ||
      url.includes('/auth/register') ||
      url.includes('/auth/forgot-password') ||
      url.includes('/auth/reset-password') ||
      url.includes('/auth/verify-email') ||
      url.includes('/auth/resend-verification') ||
      url.includes('/auth/google')
    );
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  return inject(AuthInterceptorService).intercept(req, next);
};

