import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminSubscriptionPurchase } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-subscriptions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-subscriptions.component.html',
  styleUrls: ['./admin-subscriptions.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminSubscriptionsComponent implements OnInit {
  purchases: AdminSubscriptionPurchase[] = [];
  loading = false;
  searchQuery = '';
  selectedStatus = '';
  appliedSearchQuery = '';
  appliedStatus = '';

  currentPage = 1;
  pageSize = 20;
  totalPurchases = 0;

  readonly statuses = ['pending', 'paid', 'failed'];

  constructor(
    private readonly adminService: AdminService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPurchases();
  }

  loadPurchases(): void {
    this.loading = true;
    this.cdr.markForCheck();

    this.adminService
      .getSubscriptionPurchases(this.currentPage, this.pageSize, this.appliedSearchQuery, this.appliedStatus)
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.purchases = (response.data.data || []).map((purchase) => ({
              ...purchase,
              createdAt: purchase.createdAt ? new Date(purchase.createdAt) : purchase.createdAt,
              paidAt: purchase.paidAt ? new Date(purchase.paidAt) : null,
              subscriptionEndsAt: purchase.subscriptionEndsAt ? new Date(purchase.subscriptionEndsAt) : null
            }));
            this.totalPurchases = response.data.total || 0;
          } else {
            this.purchases = [];
            this.totalPurchases = 0;
          }

          this.loading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.purchases = [];
          this.totalPurchases = 0;
          this.loading = false;
          this.cdr.markForCheck();
        }
      });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.appliedSearchQuery = this.searchQuery;
    this.appliedStatus = this.selectedStatus;
    this.loadPurchases();
  }

  onStatusFilterChange(): void {
    this.applyFilters();
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadPurchases();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadPurchases();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalPurchases / this.pageSize));
  }

  trackByPurchaseId(_index: number, purchase: AdminSubscriptionPurchase): number {
    return purchase.purchaseId;
  }

  paymentAmountDisplay(purchase: AdminSubscriptionPurchase): string {
    const amount = (purchase.amountInPaise || 0) / 100;
    return `${amount.toFixed(2)} ${purchase.currency}`;
  }
}
