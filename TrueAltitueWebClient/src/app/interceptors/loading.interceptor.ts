import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { LoaderMode, LoadingService } from '../services/loading.service';

export const LOADER_MODE = new HttpContextToken<LoaderMode>(() => 'top');

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has('X-Skip-Loader')) {
    return next(req);
  }

  const loadingService = inject(LoadingService);
  const mode = req.context.get(LOADER_MODE);
  loadingService.requestStarted(mode);

  return next(req).pipe(finalize(() => loadingService.requestEnded(mode)));
};
