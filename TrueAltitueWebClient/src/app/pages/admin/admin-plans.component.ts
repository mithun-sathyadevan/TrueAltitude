import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminSubscriptionPlan } from '../../models/admin.models';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-admin-plans',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-plans.component.html',
  styleUrls: ['./admin-plans.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPlansComponent implements OnInit {
  plans: AdminSubscriptionPlan[] = [];
  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private readonly adminService: AdminService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.cdr.markForCheck();

    this.adminService.getSubscriptionPlans().subscribe({
      next: (response) => {
        this.loading = false;

        if (response.success && response.data) {
          this.plans = response.data.length > 0
            ? response.data.map((plan) => ({ ...plan }))
            : [this.createEmptyPlan()];
        } else {
          this.errorMessage = response.message || 'Failed to load plans.';
          this.plans = [this.createEmptyPlan()];
        }

        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Failed to load plans.';
        this.plans = [this.createEmptyPlan()];
        this.cdr.markForCheck();
      },
    });
  }

  addPlan(): void {
    this.successMessage = '';
    this.errorMessage = '';
    this.plans = [...this.plans, this.createEmptyPlan()];
  }

  removePlan(index: number): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (this.plans.length <= 1) {
      this.errorMessage = 'At least one plan is required.';
      return;
    }

    this.plans = this.plans.filter((_, currentIndex) => currentIndex !== index);
  }

  savePlans(): void {
    this.successMessage = '';
    this.errorMessage = '';

    const validationError = this.validatePlans();
    if (validationError) {
      this.errorMessage = validationError;
      this.cdr.markForCheck();
      return;
    }

    this.saving = true;
    this.cdr.markForCheck();

    const payload = this.plans.map((plan) => ({
      code: plan.code.trim(),
      name: plan.name.trim(),
      priceInPaise: Number(plan.priceInPaise),
      durationDays: Number(plan.durationDays),
      description: plan.description?.trim() || '',
      isPopular: !!plan.isPopular,
    }));

    this.adminService.updateSubscriptionPlans(payload).subscribe({
      next: (response) => {
        this.saving = false;

        if (response.success && response.data) {
          this.plans = response.data.map((plan) => ({ ...plan }));
          this.successMessage = response.message || 'Plans updated successfully.';
        } else {
          this.errorMessage = response.message || 'Failed to update plans.';
        }

        this.cdr.markForCheck();
      },
      error: (error) => {
        this.saving = false;
        const serverMessage = error?.error?.message;
        this.errorMessage = typeof serverMessage === 'string' && serverMessage.trim().length > 0
          ? serverMessage
          : 'Failed to update plans.';
        this.cdr.markForCheck();
      },
    });
  }

  private validatePlans(): string {
    if (this.plans.length === 0) {
      return 'At least one plan is required.';
    }

    const codeSet = new Set<string>();

    for (let index = 0; index < this.plans.length; index++) {
      const plan = this.plans[index];
      const row = index + 1;
      const code = plan.code?.trim();
      const name = plan.name?.trim();

      if (!code || !name) {
        return `Row ${row}: Code and Name are required.`;
      }

      const normalizedCode = code.toLowerCase();
      if (codeSet.has(normalizedCode)) {
        return `Row ${row}: Duplicate code '${code}'.`;
      }
      codeSet.add(normalizedCode);

      if (!Number.isFinite(Number(plan.priceInPaise)) || Number(plan.priceInPaise) <= 0) {
        return `Row ${row}: Price (paise) must be greater than 0.`;
      }

      if (!Number.isFinite(Number(plan.durationDays)) || Number(plan.durationDays) <= 0) {
        return `Row ${row}: Duration (days) must be greater than 0.`;
      }
    }

    return '';
  }

  trackByIndex(index: number): number {
    return index;
  }

  private createEmptyPlan(): AdminSubscriptionPlan {
    return {
      code: '',
      name: '',
      priceInPaise: 0,
      durationDays: 30,
      description: '',
      isPopular: false,
    };
  }
}
