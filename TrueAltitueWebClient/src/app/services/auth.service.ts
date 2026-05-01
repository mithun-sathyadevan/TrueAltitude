import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { LOADER_MODE } from '../interceptors/loading.interceptor';

export interface AuthUser {
  id?: number;
  name: string;
  email?: string;
  avatarUrl?: string;
  provider?: 'local' | 'google';
  createdAt?: string;
}

export interface GoogleLoginResponse {
  success: boolean;
  message: string;
  token?: string;
  user?: AuthUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  avatarUrl?: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  user?: AuthUser;
}

export interface GoogleAuthResult {
  success: boolean;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userStorageKey = 'truealtitude.authUser';
  private readonly tokenStorageKey = 'truealtitude.authToken';
  private readonly legacyStorageKey = 'truealtitude.isLoggedIn';
  private readonly apiUrl = 'http://localhost:5137/api/auth';
  private readonly userSignal = signal<AuthUser | null>(this.readInitialAuthState());

  readonly currentUser = this.userSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.userSignal());

  constructor(private http: HttpClient) {}

  login(name = 'Mithun'): void {
    this.loginWithProfile({ name, provider: 'local' });
  }

  async loginWithCredentials(payload: LoginRequest): Promise<AuthResponse> {
    try {
      const response = await this.http.post<AuthResponse>(`${this.apiUrl}/login`, payload).toPromise();

      if (!response) {
        return { success: false, message: 'No response from server.' };
      }

      if (response.success && response.user) {
        this.persistAuth(response.user, response.token);
      }

      return response;
    } catch {
      return { success: false, message: 'Login failed. Please check server and credentials.' };
    }
  }

  async register(payload: RegisterRequest): Promise<AuthResponse> {
    try {
      const response = await this.http
        .post<AuthResponse>(`${this.apiUrl}/register`, payload, {
          context: new HttpContext().set(LOADER_MODE, 'blocking')
        })
        .toPromise();
      return response ?? { success: false, message: 'No response from server.' };
    } catch {
      return { success: false, message: 'Registration failed. Please check server and try again.' };
    }
  }

  loginWithProfile(profile: AuthUser): void {
    const normalizedProfile: AuthUser = {
      name: profile.name || 'Mithun',
      email: profile.email,
      avatarUrl: profile.avatarUrl,
      provider: profile.provider || 'local',
    };

    this.userSignal.set(normalizedProfile);
    localStorage.setItem(this.userStorageKey, JSON.stringify(normalizedProfile));
    localStorage.setItem(this.legacyStorageKey, 'true');
  }

  async verifyEmail(email: string, otpCode: string): Promise<AuthResponse> {
    try {
      const response = await this.http
        .post<AuthResponse>(`${this.apiUrl}/verify-email`, { email, otpCode })
        .toPromise();

      if (!response) {
        return { success: false, message: 'No response from server.' };
      }

      if (response.success && response.user) {
        this.persistAuth(response.user, response.token);
      }

      return response;
    } catch {
      return { success: false, message: 'Verification failed. Please try again.' };
    }
  }

  async resendOtp(email: string): Promise<AuthResponse> {
    try {
      const response = await this.http
        .post<AuthResponse>(`${this.apiUrl}/resend-otp`, { email })
        .toPromise();

      return response ?? { success: false, message: 'No response from server.' };
    } catch {
      return { success: false, message: 'Failed to resend OTP. Please try again.' };
    }
  }

  async loginWithGoogleToken(googleIdToken: string): Promise<GoogleAuthResult> {
    try {
      const response = await this.http
        .post<GoogleLoginResponse>(`${this.apiUrl}/login-google`, { token: googleIdToken })
        .toPromise();

      if (response?.success && response?.user) {
        this.persistAuth(response.user, response.token);

        return { success: true, message: response.message || 'Google login successful.' };
      }

      return { success: false, message: response?.message || 'Google login failed.' };
    } catch (error) {
      console.error('Google login failed:', error);
      return {
        success: false,
        message: 'Google authentication failed. Verify backend is running and CORS/origin is configured.',
      };
    }
  }

  getAuthToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  logout(): void {
    this.userSignal.set(null);
    localStorage.removeItem(this.userStorageKey);
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.legacyStorageKey);
  }

  private persistAuth(user: AuthUser, token?: string): void {
    this.userSignal.set(user);
    localStorage.setItem(this.userStorageKey, JSON.stringify(user));
    localStorage.setItem(this.legacyStorageKey, 'true');

    if (token) {
      localStorage.setItem(this.tokenStorageKey, token);
    }
  }

  private readInitialAuthState(): AuthUser | null {
    const savedUser = localStorage.getItem(this.userStorageKey);
    if (savedUser) {
      try {
        return JSON.parse(savedUser) as AuthUser;
      } catch {
        localStorage.removeItem(this.userStorageKey);
      }
    }

    if (localStorage.getItem(this.legacyStorageKey) === 'true') {
      return { name: 'Mithun', provider: 'local' };
    }

    return null;
  }
}
