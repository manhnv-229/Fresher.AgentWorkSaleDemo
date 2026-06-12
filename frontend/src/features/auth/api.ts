import { ApiError, httpJson } from '../../shared/api/http';
import type { LoginRequest, TokenResponse } from './types/auth.types';

export async function login(payload: LoginRequest): Promise<TokenResponse> {
  try {
    return await httpJson<TokenResponse, LoginRequest>('/api/auth/login', {
      method: 'POST',
      body: payload,
      credentials: 'include'
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      throw new ApiError('Email hoặc mật khẩu không đúng.', error.status, error.body);
    }

    throw error;
  }
}

export async function refreshAccessToken(): Promise<TokenResponse> {
  return httpJson<TokenResponse>('/api/auth/refresh-token', {
    method: 'POST',
    credentials: 'include'
  });
}

export async function logout(): Promise<void> {
  await httpJson<void>('/api/auth/logout', {
    method: 'POST',
    credentials: 'include'
  });
}
