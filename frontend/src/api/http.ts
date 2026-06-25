import axios, { AxiosHeaders, type AxiosRequestConfig } from 'axios';
import type { ApiErrorBody } from './api.types';
import { getAccessToken } from './interceptors';

declare module 'axios' {
  interface AxiosRequestConfig<D = any> {
    // Flag để các API module bật gắn Bearer token Authorization header. Nếu false thì request vẫn đi qua nhưng không có header Authorization.
    requiresAuth?: boolean;
  }
}

export const API_BASE_URL = (__API_BASE_URL__ || 'http://localhost:5066').replace(/\/$/, '');

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
    public readonly body?: ApiErrorBody
  ) {
    super(message);
  }
}

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true
});

apiClient.interceptors.request.use((config) => {
  if (!config.requiresAuth) {
    return config;
  }

  const accessToken = getAccessToken();
  if (!accessToken) {
    // Giữ request tiếp tục đi qua để backend tự trả 401 nếu phiên phía client đã hết.
    return config;
  }

  const headers = AxiosHeaders.from(config.headers);
  headers.set('Authorization', `Bearer ${accessToken}`);
  config.headers = headers;

  return config;
});

export async function apiRequest<TResponse, TBody = unknown>(
  config: AxiosRequestConfig<TBody>
): Promise<TResponse> {
  try {
    const response = await apiClient.request<TResponse>(config);
    if (response.status === 204) {
      // Chuẩn hóa nhánh no-content để các API module vẫn có thể dùng generic Promise<void>.
      return undefined as TResponse;
    }

    return response.data;
  } catch (error) {
    throw toApiError(error);
  }
}

function toApiError(error: unknown): ApiError {
  if (!axios.isAxiosError(error)) {
    return new ApiError('Yêu cầu không thành công. Vui lòng thử lại.');
  }

  if (!error.response) {
    // Không có response nghĩa là lỗi mạng hoặc backend không reachable từ frontend.
    return new ApiError(`Không thể kết nối API. Hãy kiểm tra backend đang chạy ở ${API_BASE_URL}.`);
  }

  const body = isApiErrorBody(error.response.data) ? error.response.data : undefined;
  return new ApiError(body?.message || 'Yêu cầu không thành công. Vui lòng thử lại.', error.response.status, body);
}

function isApiErrorBody(value: unknown): value is ApiErrorBody {
  return typeof value === 'object' && value !== null;
}
