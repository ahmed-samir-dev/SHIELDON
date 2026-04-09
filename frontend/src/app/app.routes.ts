import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
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
