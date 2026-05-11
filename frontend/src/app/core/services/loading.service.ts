import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private activeRequests = 0;

  // Expose a signal for the UI to bind to natively
  public readonly isLoading = signal(false);

  show() {
    this.activeRequests++;
    if (this.activeRequests === 1) {
      // Use queueMicrotask to avoid ExpressionChangedAfterItHasBeenCheckedError
      queueMicrotask(() => this.isLoading.set(true));
    }
  }

  hide() {
    if (this.activeRequests === 0) return;
    this.activeRequests--;
    if (this.activeRequests === 0) {
      queueMicrotask(() => this.isLoading.set(false));
    }
  }
}
