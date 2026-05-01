import { Injectable, signal } from '@angular/core';

export type LoaderMode = 'top' | 'blocking';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly activeRequests = signal(0);
  private readonly blockingRequests = signal(0);
  private readonly visible = signal(false);
  private readonly blockingVisible = signal(false);

  private readonly showDelayMs = 180;
  private readonly minVisibleMs = 350;

  private showTimer: ReturnType<typeof setTimeout> | null = null;
  private hideTimer: ReturnType<typeof setTimeout> | null = null;
  private visibleAt = 0;

  readonly isLoading = this.visible.asReadonly();
  readonly isBlockingLoading = this.blockingVisible.asReadonly();

  requestStarted(mode: LoaderMode = 'top'): void {
    this.activeRequests.update((value) => value + 1);
    if (mode === 'blocking') {
      this.blockingRequests.update((value) => value + 1);
    }

    if (this.activeRequests() === 1) {
      this.scheduleShow();
    }
  }

  requestEnded(mode: LoaderMode = 'top'): void {
    if (this.activeRequests() <= 0) {
      this.activeRequests.set(0);
      this.blockingRequests.set(0);
      this.blockingVisible.set(false);
      return;
    }

    this.activeRequests.update((value) => value - 1);
    if (mode === 'blocking') {
      this.blockingRequests.update((value) => Math.max(value - 1, 0));
    }

    if (this.blockingRequests() === 0) {
      this.blockingVisible.set(false);
    }

    if (this.activeRequests() === 0) {
      this.scheduleHide();
    }
  }

  private scheduleShow(): void {
    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
      this.hideTimer = null;
    }

    if (this.showTimer) {
      clearTimeout(this.showTimer);
    }

    this.showTimer = setTimeout(() => {
      if (this.activeRequests() > 0) {
        this.visibleAt = Date.now();
        this.visible.set(true);
        this.blockingVisible.set(this.blockingRequests() > 0);
      }
      this.showTimer = null;
    }, this.showDelayMs);
  }

  private scheduleHide(): void {
    if (this.showTimer) {
      clearTimeout(this.showTimer);
      this.showTimer = null;
    }

    if (!this.visible()) {
      return;
    }

    const elapsed = Date.now() - this.visibleAt;
    const remaining = Math.max(this.minVisibleMs - elapsed, 0);

    if (this.hideTimer) {
      clearTimeout(this.hideTimer);
    }

    this.hideTimer = setTimeout(() => {
      if (this.activeRequests() === 0) {
        this.visible.set(false);
        this.blockingVisible.set(false);
      }
      this.hideTimer = null;
    }, remaining);
  }
}
