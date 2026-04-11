import { UserRole } from './user-role.enum';

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface VerifyEmailRequest {
  email: string;
  token: string;
}

export interface ResendVerificationRequest {
  email: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface LoginResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  profilePictureUrl: string | null;
  role: UserRole;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string; // ISO 8601 UTC string
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
