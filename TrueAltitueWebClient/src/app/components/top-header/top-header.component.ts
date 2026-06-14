import { Component, ElementRef, HostListener, Input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-top-header',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './top-header.component.html',
  styleUrl: './top-header.component.scss',
})
export class TopHeaderComponent {
  @Input() name = 'Mithun';
  @Input() greeting = 'Welcome Back, Mithun';
  @Input() avatarUrl = '/images/cadet-avatar.svg';
  @Input() logoUrl = '/images/truealtitude-logo.png';

  protected isProfileMenuOpen = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly themeService: ThemeService,
    private readonly hostElement: ElementRef<HTMLElement>,
  ) {}

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (!this.isProfileMenuOpen) {
      return;
    }

    const target = event.target as Node | null;
    if (!target || !this.hostElement.nativeElement.contains(target)) {
      this.isProfileMenuOpen = false;
    }
  }

  protected isDarkMode(): boolean {
    return this.themeService.isDarkMode();
  }

  protected toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  protected isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  protected getDisplayName(): string {
    return this.authService.currentUser()?.name || this.name;
  }

  protected getDisplayAvatar(): string {
    return this.authService.currentUser()?.avatarUrl || this.avatarUrl;
  }

  protected toggleProfileMenu(): void {
    this.isProfileMenuOpen = !this.isProfileMenuOpen;
  }

  protected onLogout(): void {
    this.isProfileMenuOpen = false;
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
