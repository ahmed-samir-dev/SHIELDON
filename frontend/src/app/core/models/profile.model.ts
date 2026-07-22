import { UserRole } from './user-role.enum';

export interface UserProfileResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  profilePictureUrl: string | null;
  role: UserRole;
  displayId: string | null;
  accountStatus: string;
  createdAt: string;
  hasPassword?: boolean;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
}

export interface ChangePasswordRequest {
  currentPassword?: string;
  newPassword: string;
  confirmNewPassword?: string;
}
