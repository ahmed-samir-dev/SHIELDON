import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { UserRole } from '../models/user-role.enum';
import Shepherd, { Tour, StepOptions } from 'shepherd.js';

import { getAdminTourSteps } from '../tours/admin.tour';
import { getTutorTourSteps } from '../tours/tutor.tour';
import { getStudentTourSteps } from '../tours/student.tour';

@Injectable({
  providedIn: 'root'
})
export class ShepherdService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private tour: Tour | null = null;

  startTour(role: UserRole) {
    if (this.tour) {
      this.tour.complete();
    }

    this.tour = new Shepherd.Tour({
      defaultStepOptions: {
        cancelIcon: { enabled: true },
        classes: 'shieldon-shepherd-theme',
        scrollTo: { behavior: 'smooth', block: 'center' }
      },
      useModalOverlay: true
    });

    const steps = this.getStepsForRole(role);
    this.tour.addSteps(steps);

    // When the tour is cancelled (skipped) or completed
    this.tour.on('cancel', () => this.markTourCompleted());
    this.tour.on('complete', () => this.markTourCompleted());

    this.tour.start();
  }

  resetTour() {
    const user = this.authService.currentUser();
    if (!user) return;

    this.http.patch(`${environment.apiUrl}/profile/onboarding-reset`, {}, {
      headers: new HttpHeaders({ 'X-Silent': 'true' })
    }).subscribe({
      next: () => {
        this.authService.updateUserIdentity({ hasCompletedOnboarding: false });
        this.startTour(user.role);
      },
      error: (err) => console.error('Failed to reset tour', err)
    });
  }

  private markTourCompleted() {
    const user = this.authService.currentUser();
    if (!user || user.hasCompletedOnboarding) return; // Already completed

    // Update locally so it doesn't trigger again
    this.authService.updateUserIdentity({ hasCompletedOnboarding: true });

    // Update backend silently
    this.http.patch(`${environment.apiUrl}/profile/onboarding-complete`, {}, {
      headers: new HttpHeaders({ 'X-Silent': 'true' })
    }).subscribe({
      error: (err) => console.error('Failed to mark tour as completed', err)
    });
  }

  private getStepsForRole(role: UserRole): StepOptions[] {
    switch (role) {
      case UserRole.Admin:
        return getAdminTourSteps();
      case UserRole.Tutor:
        return getTutorTourSteps();
      case UserRole.Student:
        return getStudentTourSteps();
      default:
        return [];
    }
  }
}
