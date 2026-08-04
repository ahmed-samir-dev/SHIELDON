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
  phoneNumber?: string | null;
  phoneVerificationStatus?: 'None' | 'Unverified' | 'Verified';
  phoneVerifiedAt?: string | null;
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

export interface UpdatePhoneRequest {
  phoneNumber: string;
}

export interface SendPhoneOtpRequest {
  channel: 'whatsapp';
}

export interface VerifyPhoneOtpRequest {
  code: string;
}
