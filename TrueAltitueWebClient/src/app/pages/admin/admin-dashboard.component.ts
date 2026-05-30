import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { AdminUsersComponent } from './admin-users.component';
import { AdminSubjectsComponent } from './admin-subjects.component';
import { AdminTopicsComponent } from './admin-topics.component';
import { AdminQuestionsComponent } from './admin-questions.component';
import { AdminSubscriptionsComponent } from './admin-subscriptions.component';
import { AdminPlansComponent } from './admin-plans.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    AdminUsersComponent,
    AdminSubjectsComponent,
    AdminTopicsComponent,
    AdminQuestionsComponent
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  currentUser: any;
  activeSection: string = 'users';
  readonly subscriptionsView = AdminSubscriptionsComponent;
  readonly plansView = AdminPlansComponent;

  constructor(private authService: AuthService, private router: Router) {
    const user = this.authService.currentUser();
    this.currentUser = user;
  }

  ngOnInit(): void {
    document.body.classList.add('admin-page');

    // Check if user is admin
    const user = this.authService.currentUser();
    if (user?.role !== 'Admin') {
      this.router.navigate(['/']);
    }
  }

  ngOnDestroy(): void {
    document.body.classList.remove('admin-page');
  }

  selectSection(section: string): void {
    this.activeSection = section;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
