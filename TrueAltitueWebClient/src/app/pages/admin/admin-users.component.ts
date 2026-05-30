import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService } from '../../services/admin.service';
import { AuthService } from '../../services/auth.service';
import { AdminUser, PaginatedResponse } from '../../models/admin.models';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminUsersComponent implements OnInit {
  users: AdminUser[] = [];
  loading = false;
  currentPage = 1;
  pageSize = 20;
  totalUsers = 0;
  searchQuery = '';
  selectedRole = '';
  appliedSearchQuery = '';
  appliedRole = '';
  debugInfo = '';
  
  roles = ['Admin', 'Manager', 'Customer'];

  constructor(
    private adminService: AdminService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.cdr.markForCheck();
    this.adminService.getAllUsers(this.currentPage, this.pageSize, this.appliedSearchQuery, this.appliedRole).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          try {
            const rawUsers = response.data.data || [];
            this.users = rawUsers.map((user: any) => {
              try {
                return {
                  ...user,
                  createdAt: user.createdAt ? new Date(user.createdAt) : new Date(),
                  lastLoginAt: user.lastLoginAt ? new Date(user.lastLoginAt) : null
                };
              } catch (e) {
                return user;
              }
            });

            this.totalUsers = response.data.total || 0;
            this.debugInfo = `Loaded: ${this.users.length} | Total: ${this.totalUsers} | Search: "${this.appliedSearchQuery}" | Role: "${this.appliedRole}"`;
          } catch (e) {
            this.debugInfo = `ERROR: ${e}`;
            this.users = response.data.data || [];
            this.totalUsers = response.data.total || 0;
          }
        } else {
          this.debugInfo = `Invalid response: success=${response.success}, has data=${!!response.data}`;
        }
        
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        if (error?.status === 401) {
          this.authService.logout();
          void this.router.navigate(['/login'], { queryParams: { returnUrl: '/admin' } });
          return;
        }

        this.debugInfo = `API Error: ${error.message || error}`;
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  applyServerFilters(): void {
    this.currentPage = 1;
    this.appliedSearchQuery = this.searchQuery;
    this.appliedRole = this.selectedRole;
    this.loadUsers();
  }

  onRoleFilterChange(): void {
    this.applyServerFilters();
  }

  changeUserRole(user: AdminUser, newRole: string): void {
    if (newRole === user.role) return;

    this.adminService.updateUserRole(user.id, newRole).subscribe({
      next: () => {
        user.role = newRole;
        this.cdr.markForCheck();
        alert('User role updated successfully');
      },
      error: (error) => {
        console.error('Error updating user role:', error);
        alert('Failed to update user role');
      }
    });
  }

  toggleUserStatus(user: AdminUser): void {
    this.adminService.toggleUserStatus(user.id, !user.isActive).subscribe({
      next: () => {
        user.isActive = !user.isActive;
        this.cdr.markForCheck();
        alert('User status updated successfully');
      },
      error: (error) => {
        console.error('Error toggling user status:', error);
        alert('Failed to toggle user status');
      }
    });
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.cdr.markForCheck();
      this.loadUsers();
    }
  }

  nextPage(): void {
    const maxPage = Math.ceil(this.totalUsers / this.pageSize);
    if (this.currentPage < maxPage) {
      this.currentPage++;
      this.cdr.markForCheck();
      this.loadUsers();
    }
  }

  get totalPages(): number {
    return Math.ceil(this.totalUsers / this.pageSize);
  }

  trackByUserId(index: number, user: AdminUser): number {
    return user.id;
  }
}
