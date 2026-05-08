import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  const isSilent = req.headers.has('X-Silent');

  if (!isSilent) {
    loadingService.show();
  }

  return next(req).pipe(
    finalize(() => {
      if (!isSilent) {
        loadingService.hide();
      }
    })
  );
};
