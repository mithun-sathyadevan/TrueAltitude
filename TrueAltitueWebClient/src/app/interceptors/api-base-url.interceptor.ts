import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  // Add base URL to relative API URLs
  if (req.url.startsWith('api/')) {
    const apiUrl = environment.apiBaseUrl;
    req = req.clone({
      url: `${apiUrl}/${req.url}`
    });
  }
  return next(req);
};
