import type { ApiErrorBody } from './api.types';
import { getAccessToken } from './interceptors';

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

export async function httpJson<TResponse, TBody = unknown>(
  path: string,
  options: {
    method?: string;
    body?: TBody;
    headers?: HeadersInit;
    credentials?: RequestCredentials;
    auth?: boolean;
  } = {}
): Promise<TResponse> {
  let response: Response;
  const headers = new Headers(options.headers);

  if (options.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (options.auth) {
    const accessToken = getAccessToken();
    if (accessToken && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${accessToken}`);
    }
  }

  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method: options.method ?? 'GET',
      headers,
      credentials: options.credentials,
      body: options.body === undefined ? undefined : JSON.stringify(options.body)
    });
  } catch {
    throw new ApiError(`Không thể kết nối API. Hãy kiểm tra backend đang chạy ở ${API_BASE_URL}.`);
  }

  if (!response.ok) {
    const body = await readErrorBody(response);
    throw new ApiError(body?.message || 'Yêu cầu không thành công. Vui lòng thử lại.', response.status, body);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return response.json() as Promise<TResponse>;
}

async function readErrorBody(response: Response): Promise<ApiErrorBody | undefined> {
  try {
    return (await response.json()) as ApiErrorBody;
  } catch {
    return undefined;
  }
}
