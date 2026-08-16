import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { of, throwError, firstValueFrom } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AuthInterceptorService } from './auth.interceptor';
import { AuthService } from '../services/auth.service';

describe('AuthInterceptor Security Tests', () => {
  let interceptor: AuthInterceptorService;
  let authServiceMock: any;
  let routerMock: any;

  beforeEach(() => {
    authServiceMock = {
      getAccessToken: vi.fn(),
      getRefreshToken: vi.fn(),
      refreshAccessToken: vi.fn(),
      logout: vi.fn()
    };

    routerMock = {
      navigate: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        AuthInterceptorService,
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });

    interceptor = TestBed.inject(AuthInterceptorService);
    interceptor.resetState();
  });

  it('should attach Authorization Bearer header when token exists', async () => {
    authServiceMock.getAccessToken.mockReturnValue('valid_jwt_token_123');

    const req = new HttpRequest('GET', '/api/courses');
    let capturedHeader = '';
    const next: HttpHandlerFn = (clonedReq) => {
      capturedHeader = clonedReq.headers.get('Authorization') || '';
      return of({} as any);
    };

    await firstValueFrom(interceptor.intercept(req, next));
    expect(capturedHeader).toBe('Bearer valid_jwt_token_123');
  });

  it('should NOT attach Authorization header on login/auth routes', async () => {
    authServiceMock.getAccessToken.mockReturnValue('valid_jwt_token_123');

    const req = new HttpRequest('POST', '/api/auth/login', null);
    let hasAuthHeader = false;
    const next: HttpHandlerFn = (clonedReq) => {
      hasAuthHeader = clonedReq.headers.has('Authorization');
      return of({} as any);
    };

    await firstValueFrom(interceptor.intercept(req, next));
    expect(hasAuthHeader).toBe(false);
  });

  it('should trigger token refresh on 401 error when refresh token exists', async () => {
    authServiceMock.getAccessToken.mockReturnValue('expired_token');
    authServiceMock.getRefreshToken.mockReturnValue('valid_refresh_token');
    authServiceMock.refreshAccessToken.mockReturnValue(of({ data: { accessToken: 'new_token_456' } }));

    const req = new HttpRequest('GET', '/api/profile');
    let callCount = 0;
    let finalHeader = '';

    const next: HttpHandlerFn = (r) => {
      callCount++;
      if (callCount === 1) {
        return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }));
      }
      finalHeader = r.headers.get('Authorization') || '';
      return of({} as any);
    };

    await firstValueFrom(interceptor.intercept(req, next));
    expect(authServiceMock.refreshAccessToken).toHaveBeenCalled();
    expect(finalHeader).toBe('Bearer new_token_456');
  });

  it('should trigger logout and redirect to login if refresh fails', async () => {
    authServiceMock.getAccessToken.mockReturnValue('expired_token');
    authServiceMock.getRefreshToken.mockReturnValue('invalid_refresh_token');
    authServiceMock.refreshAccessToken.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400 })));

    const req = new HttpRequest('GET', '/api/profile');

    const next: HttpHandlerFn = () => {
      return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' }));
    };

    try {
      await firstValueFrom(interceptor.intercept(req, next));
    } catch {
      // Expected error
    }

    expect(authServiceMock.logout).toHaveBeenCalled();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });
});
