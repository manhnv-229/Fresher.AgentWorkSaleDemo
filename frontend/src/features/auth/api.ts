import { ApiError, httpJson } from '../../services/http';
import type { LoginRequest, TokenResponse } from './types/auth.types';

export async function login(payload: LoginRequest): Promise<TokenResponse> {
  try {
    return await httpJson<TokenResponse, LoginRequest>('/api/auth/login', {
      method: 'POST',
      body: payload
    });
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      throw new ApiError('Email hoặc mật khẩu không đúng.', error.status, error.body);
    }

    throw error;
  }
}
