import { CommonModule, CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { SubscriptionPlan, SubscriptionService } from '../../services/subscription.service';
import { TopHeaderComponent } from '../../components/top-header/top-header.component';

declare global {
  interface Window {
    Razorpay?: new (options: Record<string, unknown>) => { open: () => void };
  }
}

@Component({
  selector: 'app-subscription-page',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyPipe, TopHeaderComponent],
  templateUrl: './subscription-page.component.html',
  styleUrl: './subscription-page.component.scss'
})
export class SubscriptionPageComponent implements OnInit {
  readonly plans = signal<SubscriptionPlan[]>([]);
  readonly loading = signal(true);
  readonly busyPlanCode = signal('');
  readonly message = signal('');
  readonly error = signal('');

  constructor(
    private readonly subscriptionService: SubscriptionService,
    private readonly authService: AuthService
  ) {}

  async ngOnInit(): Promise<void> {
    await this.loadPlans();
  }

  protected hasActiveSubscription(): boolean {
    const user = this.authService.currentUser();
    if (!user || user.subscriptionStatus !== 'active') {
      return false;
    }

    if (!user.subscriptionExpiresAt) {
      return true;
    }

    const expiry = new Date(user.subscriptionExpiresAt).getTime();
    return !Number.isNaN(expiry) && expiry > Date.now();
  }

  protected activePlanLabel(): string {
    const user = this.authService.currentUser();
    return user?.subscriptionPlanName || user?.subscriptionPlanCode || 'No active plan';
  }

  protected activeUntilLabel(): string {
    const user = this.authService.currentUser();
    if (!user?.subscriptionExpiresAt) {
      return 'Not set';
    }

    const date = new Date(user.subscriptionExpiresAt);
    if (Number.isNaN(date.getTime())) {
      return 'Not set';
    }

    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  protected priceInInr(priceInPaise: number): number {
    return Math.round(priceInPaise / 100);
  }

  async onBuyPlan(plan: SubscriptionPlan): Promise<void> {
    this.error.set('');
    this.message.set('');
    this.busyPlanCode.set(plan.code);

    const order = await this.subscriptionService.createOrder(plan.code);
    if (!order.success || !order.providerOrderId) {
      this.error.set(order.message || 'Could not create payment order.');
      this.busyPlanCode.set('');
      return;
    }

    if (order.isMockOrder) {
      await this.finalizePayment(order.purchaseId, order.providerOrderId, `mock_pay_${Date.now()}`, 'mock_signature');
      return;
    }

    await this.ensureRazorpayLoaded();
    if (!window.Razorpay) {
      this.error.set('Razorpay SDK failed to load.');
      this.busyPlanCode.set('');
      return;
    }

    const user = this.authService.currentUser();

    const razorpay = new window.Razorpay({
      key: order.razorpayKeyId,
      amount: order.amountInPaise,
      currency: order.currency,
      name: 'TrueAltitude',
      description: `${plan.name} Subscription`,
      order_id: order.providerOrderId,
      handler: async (response: { razorpay_payment_id: string; razorpay_order_id: string; razorpay_signature: string }) => {
        await this.finalizePayment(
          order.purchaseId,
          response.razorpay_order_id,
          response.razorpay_payment_id,
          response.razorpay_signature
        );
      },
      prefill: {
        name: user?.name ?? '',
        email: user?.email ?? ''
      },
      theme: { color: '#0ea5e9' }
    });

    razorpay.open();
  }

  private async finalizePayment(
    purchaseId: number,
    providerOrderId: string,
    providerPaymentId: string,
    providerSignature: string
  ): Promise<void> {
    const result = await this.subscriptionService.verifyPayment({
      purchaseId,
      providerOrderId,
      providerPaymentId,
      providerSignature
    });

    if (!result.success) {
      this.error.set(result.message || 'Payment verification failed.');
      this.busyPlanCode.set('');
      return;
    }

    if (result.user) {
      this.authService.updateSession(result.user, result.token);
    }

    this.message.set(result.message || 'Subscription activated successfully.');
    this.busyPlanCode.set('');
  }

  private async loadPlans(): Promise<void> {
    this.loading.set(true);
    this.error.set('');

    const plans = await this.subscriptionService.getPlans();
    if (!plans.length) {
      this.error.set('Could not load plans. Please try again.');
    }

    this.plans.set(plans);
    this.loading.set(false);
  }

  private async ensureRazorpayLoaded(): Promise<void> {
    if (window.Razorpay) {
      return;
    }

    await new Promise<void>((resolve, reject) => {
      const existing = document.querySelector('script[data-razorpay="true"]') as HTMLScriptElement | null;
      if (existing) {
        existing.addEventListener('load', () => resolve(), { once: true });
        existing.addEventListener('error', () => reject(new Error('Failed to load script')), { once: true });
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.async = true;
      script.dataset['razorpay'] = 'true';
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Failed to load script'));
      document.body.appendChild(script);
    }).catch(() => {
      this.error.set('Unable to load payment gateway.');
    });
  }
}
