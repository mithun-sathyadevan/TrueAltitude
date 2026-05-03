import { AfterViewInit, Component, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../services/auth.service';

interface GoogleCredentialResponse {
  credential?: string;
}

declare global {
  interface Window {
    google?: {
      accounts?: {
        id?: {
          initialize: (options: {
            client_id: string;
            callback: (response: GoogleCredentialResponse) => void;
          }) => void;
          renderButton: (
            element: HTMLElement,
            options: { theme?: string; size?: string; text?: string; shape?: string; width?: string },
          ) => void;
          prompt: () => void;
        };
      };
    };
  }
}

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
})
export class LoginPageComponent implements AfterViewInit, OnInit {
  protected readonly appTitle = 'TrueAltitude';
  protected readonly googleError = signal('');
  protected readonly isGoogleConfigured = signal(false);
  protected email = '';
  protected password = '';
  protected errorMessage = '';
  protected showDeactivatedAccountMessage = false;
  private googleInitialized = false;

  @ViewChild('googleButtonContainer')
  private googleButtonContainer?: ElementRef<HTMLDivElement>;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const emailFromQuery = this.route.snapshot.queryParamMap.get('email');
    if (emailFromQuery) {
      this.email = decodeURIComponent(emailFromQuery);
    }

    const info = this.route.snapshot.queryParamMap.get('info');
    if (info === 'already-registered') {
      this.errorMessage = 'Email already registered. Please log in with your password.';
    }
  }

  ngAfterViewInit(): void {
    this.initGoogleLogin();
  }

  protected async onLogin(event: Event): Promise<void> {
    event.preventDefault();
    this.errorMessage = '';
    this.showDeactivatedAccountMessage = false;

    if (!this.email.trim() || !this.password) {
      this.errorMessage = 'Email and password are required.';
      return;
    }

    const result = await this.authService.loginWithCredentials({
      email: this.email.trim(),
      password: this.password,
    });

    if (!result.success) {
      // Unverified email — fresh OTP was sent, redirect to verify page
      if (result.user?.email) {
        void this.router.navigate(['/verify-email'], {
          queryParams: { email: encodeURIComponent(result.user.email) }
        });
        return;
      }

      const message = result.message || 'Login failed.';
      this.showDeactivatedAccountMessage = message.toLowerCase().includes('deactivated');
      this.errorMessage = message;
      return;
    }

    this.navigateAfterLogin();
  }

  protected onGoogleFallbackClick(event: Event): void {
    event.preventDefault();

    if (!this.isGoogleConfigured()) {
      return;
    }

    window.google?.accounts?.id?.prompt();
  }

  private initGoogleLogin(attempt = 0): void {
    if (this.googleInitialized) {
      return;
    }

    const googleClientId = environment.googleClientId || '';
    if (!googleClientId) {
      this.googleError.set('Google login is not configured yet. Update environment config with a valid Google Client ID.');
      return;
    }

    const googleIdApi = window.google?.accounts?.id;
    if (!googleIdApi || !this.googleButtonContainer) {
      if (attempt < 20) {
        setTimeout(() => this.initGoogleLogin(attempt + 1), 200);
        return;
      }

      this.googleError.set('Google SDK failed to load. Please refresh and try again.');
      return;
    }

    googleIdApi.initialize({
      client_id: googleClientId,
      callback: (response) => this.onGoogleCredential(response),
    });

    googleIdApi.renderButton(this.googleButtonContainer.nativeElement, {
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'pill',
      width: '360',
    });

    this.isGoogleConfigured.set(true);
    this.googleInitialized = true;
  }

  private async onGoogleCredential(response: GoogleCredentialResponse): Promise<void> {
    if (!response.credential) {
      this.googleError.set('No credential received from Google.');
      return;
    }

    try {
      // Send the Google ID token to the backend
      const result = await this.authService.loginWithGoogleToken(response.credential);

      if (result.success) {
        this.navigateAfterLogin();
      } else {
        this.googleError.set(result.message);
      }
    } catch (error) {
      console.error('Google authentication error:', error);
      this.googleError.set('An error occurred during Google authentication.');
    }
  }

  private navigateAfterLogin(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    void this.router.navigateByUrl(returnUrl);
  }
}
