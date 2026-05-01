import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

import { AuthService, AuthUser } from './auth.service';

export interface SubscriptionPlan {
  code: string;
  name: string;
  priceInPaise: number;
  durationDays: number;
  description?: string;
  isPopular?: boolean;
}

export interface SubscriptionOrderResponse {
  success: boolean;
  message: string;
  purchaseId: number;
  providerOrderId?: string;
  currency: string;
  amountInPaise: number;
  razorpayKeyId?: string;
  isMockOrder: boolean;
}

export interface VerifySubscriptionResponse {
  success: boolean;
  message: string;
  token?: string;
  user?: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private readonly apiUrl = 'http://localhost:5137/api/subscription';

  constructor(
    private readonly http: HttpClient,
    private readonly authService: AuthService
  ) {}

  async getPlans(): Promise<SubscriptionPlan[]> {
    try {
      const response = await this.http.get<SubscriptionPlan[]>(`${this.apiUrl}/plans`).toPromise();
      return response ?? [];
    } catch {
      return [];
    }
  }

  async createOrder(planCode: string): Promise<SubscriptionOrderResponse> {
    try {
      const token = this.authService.getAuthToken();
      const headers = token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : undefined;

      const response = await this.http
        .post<SubscriptionOrderResponse>(`${this.apiUrl}/create-order`, { planCode }, { headers })
        .toPromise();

      return response ?? {
        success: false,
        message: 'No response from server.',
        purchaseId: 0,
        currency: 'INR',
        amountInPaise: 0,
        isMockOrder: false,
      };
    } catch {
      return {
        success: false,
        message: 'Failed to create subscription order.',
        purchaseId: 0,
        currency: 'INR',
        amountInPaise: 0,
        isMockOrder: false,
      };
    }
  }

  async verifyPayment(payload: {
    purchaseId: number;
    providerOrderId: string;
    providerPaymentId: string;
    providerSignature: string;
  }): Promise<VerifySubscriptionResponse> {
    try {
      const token = this.authService.getAuthToken();
      const headers = token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : undefined;

      const response = await this.http
        .post<VerifySubscriptionResponse>(`${this.apiUrl}/verify-payment`, payload, { headers })
        .toPromise();

      return response ?? { success: false, message: 'No response from server.' };
    } catch {
      return { success: false, message: 'Failed to verify payment.' };
    }
  }
}
