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
        path: 'forgot-password',
        // Placeholder — will be implemented in Stage 1.4
        loadComponent: () => import('./features/auth/login/login').then(m => m.Login),
        title: 'Forgot Password — SHIELDON'
      }
    ]
  },

  // ── Authenticated Routes (Protected by authGuard) ─────────────────────────
  // Placeholder dashboards — will be fleshed out in Phase 2+
  {
    path: 'student',
    canActivate: [deviceGuard, authGuard],
    loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout),
    children: [
      { path: 'dashboard', loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout), title: 'Student Dashboard — SHIELDON' }
    ]
  },
  {
    path: 'tutor',
    canActivate: [deviceGuard, authGuard],
    loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout),
    children: [
      { path: 'dashboard', loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout), title: 'Tutor Dashboard — SHIELDON' }
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
