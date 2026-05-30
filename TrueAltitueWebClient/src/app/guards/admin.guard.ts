import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = (_, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return (async () => {
    if (!authService.isLoggedIn()) {
      authService.logout();
      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url },
      });
    }

    const token = await authService.getValidAccessToken();
    if (!token) {
      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url },
      });
    }

    const user = authService.currentUser();
    if ((user?.role || '').toLowerCase() === 'admin') {
      return true;
    }

    return router.createUrlTree(['/']);
  })();
};
