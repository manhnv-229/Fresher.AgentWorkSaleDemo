import { ApiError, httpJson } from './http';
import type { ChangePasswordRequest, LoginRequest, TokenResponse } from './auth.types';

export async function login(payload: LoginRequest): Promise<TokenResponse> {
  try {
    return await httpJson<TokenResponse, LoginRequest>('/api/auth/login', {
      method: 'POST',
      body: payload,
      credentials: 'include'
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      if (error.body?.code === 'locked_account') {
        throw new ApiError('Tài khoản đang bị khóa.', error.status, error.body);
      }

      throw new ApiError('Email hoặc mật khẩu không đúng.', error.status, error.body);
    }

    throw error;
  }
}

export async function changePassword(payload: ChangePasswordRequest): Promise<void> {
  try {
    await httpJson<void, ChangePasswordRequest>('/api/auth/change-password', {
      method: 'POST',
      body: payload,
      credentials: 'include',
      auth: true
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 400 && error.body?.code === 'invalid_current_password') {
      throw new ApiError('Mật khẩu hiện tại không đúng.', error.status, error.body);
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
