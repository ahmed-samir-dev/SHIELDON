import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError, switchMap, BehaviorSubject, filter, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
const refreshSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip auth endpoints to avoid infinite loops
  if (isAuthRoute(req.url)) {
    return next(req);
  }

  const token = authService.getAccessToken();
  const authReq = token ? addToken(req, token) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && authService.getRefreshToken()) {
        return handle401Error(req, next, authService, router);
      }
      return throwError(() => error);
    })
  );
};

function handle401Error(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService,
  router: Router
) {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshSubject.next(null);

    return authService.refreshAccessToken().pipe(
      switchMap(response => {
        isRefreshing = false;
        const newToken = response.data.accessToken;
        refreshSubject.next(newToken);
        return next(addToken(req, newToken));
      }),
      catchError(err => {
        isRefreshing = false;
        authService.logout();
        router.navigate(['/login']);
        return throwError(() => err);
      })
    );
  }

  // If already refreshing, queue subsequent requests to wait for the new token
  return refreshSubject.pipe(
    filter(token => token !== null),
    take(1),
    switchMap(token => next(addToken(req, token!)))
  );
}

function addToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });
}

function isAuthRoute(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/refresh');
}
