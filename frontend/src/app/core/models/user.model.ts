export type UserRole = 'Admin' | 'Tutor' | 'Student';
export type AccountStatus = 'Active' | 'Locked' | 'Unverified' | 'Disabled';

export interface UserDetailDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  profilePictureUrl: string | null;
  role: UserRole;
  studentId: string | null;
  tutorId: string | null;
  accountStatus: AccountStatus;
  failedLoginAttempts: number;
  lockedAt: string | null;
  emailVerifiedAt: string | null;
  lastLoginAt: string | null;
  hasCompletedOnboarding: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UserFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  role?: string;
  status?: string;
  sortColumn?: string;
  sortDirection?: string;
}
