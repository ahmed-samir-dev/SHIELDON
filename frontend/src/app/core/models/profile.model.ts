import { UserRole } from './user-role.enum';

export interface UserProfileResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  profilePictureUrl: string | null;
  role: UserRole;
  createdAt: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
}
