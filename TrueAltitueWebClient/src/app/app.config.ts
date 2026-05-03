import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { authInterceptor } from './interceptors/auth.interceptor';
import { apiBaseUrlInterceptor } from './interceptors/api-base-url.interceptor';
import { loadingInterceptor } from './interceptors/loading.interceptor';
import { requestTimeInterceptor } from './interceptors/request-time.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([requestTimeInterceptor, authInterceptor, apiBaseUrlInterceptor, loadingInterceptor])),
    provideRouter(routes)
  ],
};
