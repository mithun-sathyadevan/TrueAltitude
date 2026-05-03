import { HttpInterceptorFn } from '@angular/common/http';

export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  // Add base URL to relative API URLs
  if (req.url.startsWith('api/')) {
    const apiUrl = 'http://localhost:5137';
    req = req.clone({
      url: `${apiUrl}/${req.url}`
    });
  }
  return next(req);
};
