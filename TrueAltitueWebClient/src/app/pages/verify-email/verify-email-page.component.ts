import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-verify-email-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="verify-container">
      <div class="verify-card">
        <div class="verify-icon">✉️</div>
        <h1>Verify Your Email</h1>
        <p class="subtitle">
          We sent a 6-digit code to <strong>{{ email() }}</strong>.
          Enter it below to activate your account.
        </p>

        <form (ngSubmit)="onVerify()" #form="ngForm">
          <div class="field">
            <label for="otp">Verification Code</label>
            <input
              id="otp"
              type="text"
              [(ngModel)]="otpCode"
              name="otpCode"
              maxlength="6"
              inputmode="numeric"
              autocomplete="one-time-code"
              placeholder="000000"
              required
            />
          </div>

          <div *ngIf="errorMessage()" class="error-message">{{ errorMessage() }}</div>
          <div *ngIf="successMessage()" class="success-message">{{ successMessage() }}</div>

          <button type="submit" class="btn-primary" [disabled]="isLoading()">
            {{ isLoading() ? 'Verifying...' : 'Verify Email' }}
          </button>
        </form>

        <div class="resend-section">
          <span>Didn't receive the code?</span>
          <button
            class="btn-link"
            (click)="onResend()"
            [disabled]="resendCooldown() > 0 || isLoading()"
          >
            {{ resendCooldown() > 0 ? 'Resend in ' + resendCooldown() + 's' : 'Resend Code' }}
          </button>
        </div>

        <div class="back-link">
          <a routerLink="/login">Back to Login</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .verify-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
      padding: 2rem;
    }

    .verify-card {
      background: white;
      border-radius: 16px;
      padding: 2.5rem;
      width: 100%;
      max-width: 420px;
      text-align: center;
      box-shadow: 0 20px 60px rgba(0,0,0,0.3);
    }

    .verify-icon {
      font-size: 3rem;
      margin-bottom: 1rem;
    }

    h1 {
      font-size: 1.75rem;
      font-weight: 700;
      color: #0f172a;
      margin: 0 0 0.5rem;
    }

    .subtitle {
      color: #64748b;
      margin-bottom: 2rem;
      line-height: 1.5;
    }

    .field {
      text-align: left;
      margin-bottom: 1.5rem;
    }

    label {
      display: block;
      font-size: 0.875rem;
      font-weight: 600;
      color: #374151;
      margin-bottom: 0.5rem;
    }

    input {
      width: 100%;
      padding: 0.875rem 1rem;
      border: 2px solid #e2e8f0;
      border-radius: 10px;
      font-size: 1.5rem;
      letter-spacing: 0.5rem;
      text-align: center;
      outline: none;
      transition: border-color 0.2s;
      box-sizing: border-box;
    }

    input:focus {
      border-color: #6366f1;
    }

    .error-message {
      color: #ef4444;
      font-size: 0.875rem;
      margin-bottom: 1rem;
      padding: 0.75rem;
      background: #fef2f2;
      border-radius: 8px;
    }

    .success-message {
      color: #22c55e;
      font-size: 0.875rem;
      margin-bottom: 1rem;
      padding: 0.75rem;
      background: #f0fdf4;
      border-radius: 8px;
    }

    .btn-primary {
      width: 100%;
      padding: 0.875rem;
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: white;
      border: none;
      border-radius: 10px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: opacity 0.2s;
    }

    .btn-primary:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .resend-section {
      margin-top: 1.5rem;
      color: #64748b;
      font-size: 0.875rem;
      display: flex;
      gap: 0.5rem;
      align-items: center;
      justify-content: center;
    }

    .btn-link {
      background: none;
      border: none;
      color: #6366f1;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      padding: 0;
      text-decoration: underline;
    }

    .btn-link:disabled {
      color: #94a3b8;
      cursor: not-allowed;
      text-decoration: none;
    }

    .back-link {
      margin-top: 1rem;
    }

    .back-link a {
      color: #64748b;
      font-size: 0.875rem;
      text-decoration: none;
    }

    .back-link a:hover {
      color: #6366f1;
    }
  `]
})
export class VerifyEmailPageComponent implements OnInit {
  email = signal('');
  errorMessage = signal('');
  successMessage = signal('');
  isLoading = signal(false);
  resendCooldown = signal(0);

  otpCode = '';
  private cooldownInterval: ReturnType<typeof setInterval> | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const emailParam = this.route.snapshot.queryParamMap.get('email');
    if (emailParam) {
      this.email.set(decodeURIComponent(emailParam));
    } else {
      this.router.navigate(['/register']);
    }
  }

  async onVerify(): Promise<void> {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.otpCode.length !== 6) {
      this.errorMessage.set('Please enter the 6-digit code.');
      return;
    }

    this.isLoading.set(true);
    const result = await this.authService.verifyEmail(this.email(), this.otpCode);
    this.isLoading.set(false);

    if (result.success) {
      this.successMessage.set('Email verified! Redirecting…');
      setTimeout(() => this.router.navigate(['/']), 1500);
    } else {
      this.errorMessage.set(result.message || 'Verification failed.');
    }
  }

  async onResend(): Promise<void> {
    this.errorMessage.set('');
    this.successMessage.set('');
    this.isLoading.set(true);

    const result = await this.authService.resendOtp(this.email());
    this.isLoading.set(false);

    if (result.success) {
      this.successMessage.set('A new code has been sent to your email.');
      this.startCooldown(60);
    } else {
      this.errorMessage.set(result.message || 'Failed to resend code.');
    }
  }

  private startCooldown(seconds: number): void {
    this.resendCooldown.set(seconds);
    this.cooldownInterval = setInterval(() => {
      const current = this.resendCooldown();
      if (current <= 1) {
        this.resendCooldown.set(0);
        if (this.cooldownInterval) clearInterval(this.cooldownInterval);
      } else {
        this.resendCooldown.set(current - 1);
      }
    }, 1000);
  }
}
