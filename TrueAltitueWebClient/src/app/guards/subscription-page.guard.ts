import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

export const subscriptionPageGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const user = authService.currentUser();

  if (!user || user.subscriptionStatus !== 'active') {
    return true;
  }

  if (!user.subscriptionExpiresAt) {
    return router.createUrlTree(['/']);
  }

  const expiry = new Date(user.subscriptionExpiresAt).getTime();
  if (!Number.isNaN(expiry) && expiry > Date.now()) {
    return router.createUrlTree(['/']);
  }

  return true;
};
