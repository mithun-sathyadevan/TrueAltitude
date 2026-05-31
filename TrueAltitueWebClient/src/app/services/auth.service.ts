import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { LOADER_MODE } from '../interceptors/loading.interceptor';
import { environment } from '../../environments/environment';

export interface AuthUser {
  id?: number;
  name: string;
  email?: string;
  avatarUrl?: string;
  provider?: 'local' | 'google';
  role?: string;
  subscriptionStatus?: 'none' | 'active' | 'expired';
  subscriptionPlanCode?: string;
  subscriptionPlanName?: string;
  subscriptionStartedAt?: string;
  subscriptionExpiresAt?: string;
  createdAt?: string;
}

export interface GoogleLoginResponse {
  success: boolean;
  message: string;
  token?: string;
  refreshToken?: string;
  refreshTokenExpiresAt?: string;
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
  refreshToken?: string;
  refreshTokenExpiresAt?: string;
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
  private readonly refreshTokenStorageKey = 'truealtitude.refreshToken';
  private readonly refreshTokenExpiryStorageKey = 'truealtitude.refreshTokenExpiry';
  private readonly legacyStorageKey = 'truealtitude.isLoggedIn';
  private readonly apiUrl = `${environment.apiBaseUrl}/api/auth`;
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
        this.persistAuth(response.user, response.token, response.refreshToken, response.refreshTokenExpiresAt);
      }

      return response;
    } catch (error: any) {
      const backendMessage = error?.error?.message || error?.error?.Message;
      return {
        success: false,
        message: backendMessage || 'Login failed. Please check server and credentials.',
      };
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
        this.persistAuth(response.user, response.token, response.refreshToken, response.refreshTokenExpiresAt);
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
        this.persistAuth(response.user, response.token, response.refreshToken, response.refreshTokenExpiresAt);

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

  async getValidAccessToken(): Promise<string | null> {
    const token = this.getAuthToken();
    if (token && !this.isJwtExpired(token)) {
      return token;
    }

    const refreshed = await this.refreshAccessToken();
    if (refreshed) {
      const newToken = this.getAuthToken();
      if (newToken && !this.isJwtExpired(newToken)) {
        return newToken;
      }
    }

    this.logout();
    return null;
  }

  async refreshAccessToken(): Promise<boolean> {
    const refreshToken = localStorage.getItem(this.refreshTokenStorageKey);
    const refreshTokenExpiry = localStorage.getItem(this.refreshTokenExpiryStorageKey);

    if (!refreshToken || !refreshTokenExpiry) {
      return false;
    }

    const expiryTime = new Date(refreshTokenExpiry).getTime();
    if (Number.isNaN(expiryTime) || expiryTime <= Date.now()) {
      this.logout();
      return false;
    }

    try {
      const response = await this.http
        .post<AuthResponse>(`${this.apiUrl}/refresh-token`, { refreshToken })
        .toPromise();

      if (!response?.success || !response.token || !response.user) {
        this.logout();
        return false;
      }

      this.persistAuth(response.user, response.token, response.refreshToken, response.refreshTokenExpiresAt);
      return true;
    } catch {
      this.logout();
      return false;
    }
  }

  updateSession(user: AuthUser, token?: string): void {
    this.persistAuth(user, token);
  }

  logout(): void {
    this.userSignal.set(null);
    localStorage.removeItem(this.userStorageKey);
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.refreshTokenStorageKey);
    localStorage.removeItem(this.refreshTokenExpiryStorageKey);
    localStorage.removeItem(this.legacyStorageKey);
  }

  private persistAuth(user: AuthUser, token?: string, refreshToken?: string, refreshTokenExpiresAt?: string): void {
    this.userSignal.set(user);
    localStorage.setItem(this.userStorageKey, JSON.stringify(user));
    localStorage.setItem(this.legacyStorageKey, 'true');

    if (token) {
      localStorage.setItem(this.tokenStorageKey, token);
    }

    if (refreshToken) {
      localStorage.setItem(this.refreshTokenStorageKey, refreshToken);
    }

    if (refreshTokenExpiresAt) {
      localStorage.setItem(this.refreshTokenExpiryStorageKey, refreshTokenExpiresAt);
    }
  }

  private isJwtExpired(token: string): boolean {
    try {
      const payloadSegment = token.split('.')[1];
      if (!payloadSegment) {
        return true;
      }

      const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(normalized.length + (4 - (normalized.length % 4 || 4)) % 4, '=');
      const payload = JSON.parse(atob(padded));
      const exp = Number(payload?.exp);
      if (!exp) {
        return true;
      }

      return Date.now() >= exp * 1000;
    } catch {
      return true;
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
