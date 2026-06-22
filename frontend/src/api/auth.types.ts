export interface LoginRequest {
  email: string;
  password: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface TokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface AuthState {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  receivedAt: string;
}

export interface AdminUserSummary {
  id: string;
  email: string;
  fullName: string | null;
  status: string;
  passwordChangedAt: string | null;
  employeeCode: string | null;
  project: string | null;
  jobPosition: string | null;
}
