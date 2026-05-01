import { Injectable, computed, signal } from '@angular/core';

export interface AuthUser {
  name: string;
  email?: string;
  avatarUrl?: string;
  provider?: 'local' | 'google';
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userStorageKey = 'truealtitude.authUser';
  private readonly legacyStorageKey = 'truealtitude.isLoggedIn';
  private readonly userSignal = signal<AuthUser | null>(this.readInitialAuthState());

  readonly currentUser = this.userSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.userSignal());

  login(name = 'Mithun'): void {
    this.loginWithProfile({ name, provider: 'local' });
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

  logout(): void {
    this.userSignal.set(null);
    localStorage.removeItem(this.userStorageKey);
    localStorage.removeItem(this.legacyStorageKey);
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
