import { UserRole } from './user-role.enum';

export interface LoginRequest {
  email: string;
  password: string;
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
