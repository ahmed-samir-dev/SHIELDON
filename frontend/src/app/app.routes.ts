import { Routes } from '@angular/router';
import { deviceGuard } from './core/guards/device.guard';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // ── Mobile Guard ──────────────────────────────────────────────────────────
  {
    path: 'mobile-blocked',
    loadComponent: () => import('./features/public/mobile-blocked/mobile-blocked').then(m => m.MobileBlocked),
    title: 'Mobile Access Restricted - SHIELDON'
  },

  // ── Public Routes (Guest) ─────────────────────────────────────────────────
  {
    path: '',
    canActivate: [deviceGuard],
    loadComponent: () => import('./layouts/public-layout/public-layout').then(m => m.PublicLayout),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/public/landing/landing').then(m => m.Landing),
        title: 'SHIELDON — Next-Gen LMS & Anti-Cheating Engine'
      },
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then(m => m.Login),
        title: 'Login — SHIELDON'
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register').then(m => m.Register),
        title: 'Register — SHIELDON'
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./features/auth/forgot-password/forgot-password').then(m => m.ForgotPassword),
        title: 'Forgot Password — SHIELDON'
      }
    ]
  },

  // ── Auth Verification Routes (Standalone) ─────────────────────────────────
  {
    path: 'auth/verify-email',
    canActivate: [deviceGuard],
    loadComponent: () => import('./features/auth/verify-email/verify-email').then(m => m.VerifyEmail),
    title: 'Verify Email — SHIELDON'
  },
  {
    path: 'auth/reset-password',
    canActivate: [deviceGuard],
    loadComponent: () => import('./features/auth/reset-password/reset-password').then(m => m.ResetPassword),
    title: 'Reset Password — SHIELDON'
  },

  // ── Authenticated Routes (Protected by authGuard) ─────────────────────────
  {
    path: '',
    canActivate: [deviceGuard, authGuard],
    loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout),
    children: [
      { path: '', redirectTo: 'profile', pathMatch: 'full' },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile').then(m => m.ProfileComponent),
        title: 'My Profile — SHIELDON'
      },
      {
        path: 'courses',
        loadComponent: () => import('./features/courses/course-list/course-list').then(m => m.CourseList),
        title: 'Manage Courses — SHIELDON'
      },
      {
        path: 'courses/:id',
        loadComponent: () => import('./features/courses/course-detail/course-detail').then(m => m.CourseDetail),
        title: 'Course Hub — SHIELDON'
      },
      {
        path: 'enrollments',
        loadComponent: () => import('./features/courses/enrollment-panel/enrollment-panel').then(m => m.EnrollmentPanel),
        title: 'Enrollment Requests — SHIELDON'
      },
      // Dashboards fallback to courses view for now
      { path: 'admin/dashboard', redirectTo: 'courses' },
      { path: 'student/dashboard', redirectTo: 'courses' },
      { path: 'tutor/dashboard', redirectTo: 'courses' }
    ]
  },

  // ── Fallback ───────────────────────────────────────────────────────────────
  { path: '**', redirectTo: '' }
];
