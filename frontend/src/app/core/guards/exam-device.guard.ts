import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const examDeviceGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);

  // Exclude the blocked route itself from infinite redirects
  if (state.url.includes('/exam-device-blocked')) {
    return true;
  }

  // Detect mobile user agent or small screen width (< 1024px)
  const isMobileUa = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
  const isSmallScreen = window.innerWidth < 1024;

  if (isMobileUa || isSmallScreen) {
    return router.parseUrl('/exam-device-blocked');
  }

  return true;
};
