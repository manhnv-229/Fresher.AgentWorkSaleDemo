import { ApiError, apiRequest } from './http';
import type { ChangePasswordRequest, LoginRequest, TokenResponse } from './auth.types';

export async function login(payload: LoginRequest): Promise<TokenResponse> {
  try {
    return await apiRequest<TokenResponse, LoginRequest>({
      url: '/api/auth/login',
      method: 'POST',
      data: payload
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      if (error.body?.code === 'locked_account') {
        throw new ApiError('Tài khoản đang bị khóa.', error.status, error.body);
      }

      // Giữ mapping message tại đây để UI login không phải biết chi tiết mã lỗi backend.
      throw new ApiError('Email hoặc mật khẩu không đúng.', error.status, error.body);
    }

    throw error;
  }
}

export async function changePassword(payload: ChangePasswordRequest): Promise<void> {
  try {
    await apiRequest<void, ChangePasswordRequest>({
      url: '/api/auth/change-password',
      method: 'POST',
      data: payload,
      requiresAuth: true
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 400 && error.body?.code === 'invalid_current_password') {
      // Chuyển mã lỗi kỹ thuật sang message hiển thị ổn định cho form đổi mật khẩu.
      throw new ApiError('Mật khẩu hiện tại không đúng.', error.status, error.body);
    }

    throw error;
  }
}

export async function refreshAccessToken(): Promise<TokenResponse> {
  return apiRequest<TokenResponse>({
    url: '/api/auth/refresh-token',
    method: 'POST',
  });
}

export async function logout(): Promise<void> {
  await apiRequest<void>({
    url: '/api/auth/logout',
    method: 'POST',
  });
}
