import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserProfileResponse, UpdateProfileRequest } from '../models/profile.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  getProfile(): Observable<{ data: UserProfileResponse, message: string }> {
    return this.http.get<{ data: UserProfileResponse, message: string }>(`${environment.apiUrl}/profile`).pipe(
      tap(res => this.updateAuthIdentity(res.data))
    );
  }

  updateProfile(request: UpdateProfileRequest): Observable<{ data: UserProfileResponse, message: string }> {
    return this.http.patch<{ data: UserProfileResponse, message: string }>(`${environment.apiUrl}/profile`, request).pipe(
      tap(res => this.updateAuthIdentity(res.data))
    );
  }

  uploadProfilePicture(file: File): Observable<{ data: UserProfileResponse, message: string }> {
    const formData = new FormData();
    formData.append('file', file);
    
    return this.http.post<{ data: UserProfileResponse, message: string }>(`${environment.apiUrl}/profile/picture`, formData).pipe(
      tap(res => this.updateAuthIdentity(res.data))
    );
  }

  // ── Sync with Auth Identity ──────────────────────────────────────────────
  // When the profile is updated, we proactively update the currentUser signal 
  // so the Avatar in the NavBar updates immediately everywhere.
  private updateAuthIdentity(profile: UserProfileResponse) {
    const current = this.authService.currentUser();
    if (current) {
      this.authService.updateUserIdentity({
        firstName: profile.firstName,
        lastName: profile.lastName,
        profilePictureUrl: profile.profilePictureUrl
      });
    }
  }
}
