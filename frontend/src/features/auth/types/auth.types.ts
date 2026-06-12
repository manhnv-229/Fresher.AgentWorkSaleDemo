export interface LoginRequest {
  email: string;
  password: string;
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
