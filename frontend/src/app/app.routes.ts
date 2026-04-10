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
      }
    ]
  },

  // ── Auth Standalone Routes ────────────────────────────────────────────────
  {
    path: 'login',
    canActivate: [deviceGuard],
    loadComponent: () => import('./features/auth/login/login').then(m => m.Login),
    title: 'Login — SHIELDON'
  },
  {
    path: 'forgot-password',
    canActivate: [deviceGuard],
    loadComponent: () => import('./features/auth/forgot-password/forgot-password').then(m => m.ForgotPassword),
    title: 'Forgot Password — SHIELDON'
  },
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
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile').then(m => m.ProfileComponent),
        title: 'My Profile — SHIELDON'
      },
      // Keep old placeholders for testing
      { path: 'student/dashboard', loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout) }
    ]
  },
  {

    path: 'admin',
    canActivate: [deviceGuard, authGuard],
    loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout),
    children: [
      { path: 'dashboard', loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout), title: 'Admin Dashboard — SHIELDON' }
    ]
  },

  // ── Fallback ───────────────────────────────────────────────────────────────
  { path: '**', redirectTo: '' }
];
