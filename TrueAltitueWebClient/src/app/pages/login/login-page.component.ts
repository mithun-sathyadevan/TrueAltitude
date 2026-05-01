import { AfterViewInit, Component, ElementRef, ViewChild, signal } from '@angular/core';
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
  imports: [RouterLink],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss',
})
export class LoginPageComponent implements AfterViewInit {
  protected readonly appTitle = 'TrueAltitude';
  protected readonly googleError = signal('');
  protected readonly isGoogleConfigured = signal(false);

  @ViewChild('googleButtonContainer')
  private googleButtonContainer?: ElementRef<HTMLDivElement>;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  ngAfterViewInit(): void {
    this.initGoogleLogin();
  }

  protected onLogin(event: Event): void {
    event.preventDefault();
    this.authService.login('Mithun');

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
  }

  private onGoogleCredential(response: GoogleCredentialResponse): void {
    const profile = this.decodeJwtPayload(response.credential);
    this.authService.loginWithProfile({
      name: profile?.name || 'Mithun',
      email: profile?.email,
      avatarUrl: profile?.picture,
      provider: 'google',
    });

    this.navigateAfterLogin();
  }

  private decodeJwtPayload(token?: string):
    | { name?: string; email?: string; picture?: string }
    | undefined {
    if (!token) {
      return undefined;
    }

    try {
      const payloadSegment = token.split('.')[1];
      if (!payloadSegment) {
        return undefined;
      }

      const base64 = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(base64)
          .split('')
          .map((char) => `%${`00${char.charCodeAt(0).toString(16)}`.slice(-2)}`)
          .join(''),
      );

      return JSON.parse(json) as { name?: string; email?: string; picture?: string };
    } catch {
      return undefined;
    }
  }

  private navigateAfterLogin(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    void this.router.navigateByUrl(returnUrl);
  }
}
