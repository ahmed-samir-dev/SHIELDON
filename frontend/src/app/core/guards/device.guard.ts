import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const deviceGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  // Exclude the blocked route from infinite redirects
  if (state.url.includes('/mobile-blocked')) {
    return true;
  }

  // Detect mobile device
  const isMobileUa = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
  const isSmallScreen = window.innerWidth <= 768;

  // We enforce desktop for SHIELDON (Remote Proctoring needs large screens/keyboard context)
  if (isMobileUa || isSmallScreen) {
    return router.parseUrl('/mobile-blocked');
  }

  return true;
};
