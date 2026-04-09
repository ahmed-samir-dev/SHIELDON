import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  // Exclude specific URLs from triggering the global loading bar (e.g. background polling)
  const isExcluded = req.url.includes('/api/monitoring/heartbeat');
  
  if (!isExcluded) {
    loadingService.show();
  }

  return next(req).pipe(
    finalize(() => {
      if (!isExcluded) {
        loadingService.hide();
      }
    })
  );
};
