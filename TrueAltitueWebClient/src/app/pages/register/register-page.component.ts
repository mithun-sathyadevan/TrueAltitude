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

  protected onRegister(event: Event): void {
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

    // Perform registration with local login
    this.authService.loginWithProfile({
      name: this.name,
      email: this.email,
      provider: 'local',
    });

    this.successMessage = 'Registration successful! Redirecting...';

    setTimeout(() => {
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
      void this.router.navigateByUrl(returnUrl);
    }, 1000);
  }
}
