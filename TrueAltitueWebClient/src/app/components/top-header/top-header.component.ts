import { Component, Input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../services/auth.service';

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
  @Input() logoUrl = '/images/truealtitude-logo.jpg';

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  protected isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  protected getDisplayName(): string {
    return this.authService.currentUser()?.name || this.name;
  }

  protected getDisplayAvatar(): string {
    return this.authService.currentUser()?.avatarUrl || this.avatarUrl;
  }

  protected onLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
