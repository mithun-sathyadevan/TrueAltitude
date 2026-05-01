import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SubscriptionAccessService {
  constructor(private readonly authService: AuthService) {}

  hasActiveSubscription(): boolean {
    const user = this.authService.currentUser();
    if (!user) {
      return false;
    }

    if (user.subscriptionStatus !== 'active') {
      return false;
    }

    if (!user.subscriptionExpiresAt) {
      return true;
    }

    const expiry = new Date(user.subscriptionExpiresAt).getTime();
    return !Number.isNaN(expiry) && expiry > Date.now();
  }

  redirectToSubscription(router: Router, returnUrl?: string): void {
    void router.navigate(['/subscriptions'], {
      queryParams: returnUrl ? { returnUrl } : undefined,
    });
  }
}
