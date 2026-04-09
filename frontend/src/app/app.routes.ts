import { Routes } from '@angular/router';
import { deviceGuard } from './core/guards/device.guard';

export const routes: Routes = [
  {
    path: 'mobile-blocked',
    loadComponent: () => import('./features/public/mobile-blocked/mobile-blocked').then(m => m.MobileBlocked),
    title: 'Mobile Access Restricted - SHIELDON'
  },
  {
    path: '',
    canActivate: [deviceGuard],
    loadComponent: () => import('./layouts/public-layout/public-layout').then(m => m.PublicLayout),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/public/landing/landing').then(m => m.Landing),
        title: 'SHIELDON - Next-Gen LMS & Anti-Cheating Engine'
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
