import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const isAuthApiCall = req.url.includes('/api/auth/login')
    || req.url.includes('/api/auth/register')
    || req.url.includes('/api/auth/login-google')
    || req.url.includes('/api/auth/verify-email')
    || req.url.includes('/api/auth/resend-otp')
    || req.url.includes('/api/auth/refresh-token');

  if (isAuthApiCall || req.headers.has('Authorization')) {
    return next(req);
  }

  return from(authService.getValidAccessToken()).pipe(
    switchMap((token) => {
      if (!token) {
        return next(req);
      }

      const authenticatedRequest = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });

      return next(authenticatedRequest);
    })
  );
};
