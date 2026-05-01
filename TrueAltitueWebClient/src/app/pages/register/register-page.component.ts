import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './register-page.component.html',
  styleUrl: './register-page.component.scss',
})
export class RegisterPageComponent {
  protected readonly appTitle = 'TrueAltitude';
  protected name = '';
  protected email = '';
  protected password = '';
  protected confirmPassword = '';
  protected errorMessage = '';
  protected successMessage = '';

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  protected async onRegister(event: Event): Promise<void> {
    event.preventDefault();
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.name.trim()) {
      this.errorMessage = 'Name is required.';
      return;
    }

    if (!this.email.trim()) {
      this.errorMessage = 'Email is required.';
      return;
    }

    if (!this.password) {
      this.errorMessage = 'Password is required.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Passwords do not match.';
      return;
    }

    if (this.password.length < 6) {
      this.errorMessage = 'Password must be at least 6 characters.';
      return;
    }

    const response = await this.authService.register({
      name: this.name.trim(),
      email: this.email.trim(),
      password: this.password,
    });

    if (!response.success) {
      const isAlreadyRegistered = (response.message || '').toLowerCase().includes('email already registered');
      if (isAlreadyRegistered) {
        void this.router.navigate(['/login'], {
          queryParams: {
            email: encodeURIComponent(this.email.trim()),
            info: 'already-registered'
          }
        });
        return;
      }

      this.errorMessage = response.message || 'Registration failed.';
      return;
    }

    // success=true covers both new registrations and returning unverified users (OTP resent)
    this.successMessage = response.message || 'Please check your email for a verification code.';

    setTimeout(() => {
      void this.router.navigate(['/verify-email'], {
        queryParams: { email: encodeURIComponent(this.email.trim()) }
      });
    }, 1000);
  }
}
