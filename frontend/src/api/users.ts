import { apiRequest } from './http';
import type { AdminUserSummary } from './auth.types';
import type { PagedResult } from './agents';

// Bộ lọc cho màn quản trị thành viên/người dùng.
export interface MemberListFilters {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

// Lấy danh sách người dùng quản trị với bộ lọc và phân trang.
export async function getUsers(filters?: MemberListFilters): Promise<PagedResult<AdminUserSummary>> {
  const params = new URLSearchParams();
  if (filters?.search) params.set('search', filters.search);
  if (filters?.status) params.set('status', filters.status);
  if (filters?.page) params.set('page', String(filters.page));
  if (filters?.pageSize) params.set('pageSize', String(filters.pageSize));

  const query = params.toString();
  const url = `/api/admin/users${query ? `?${query}` : ''}`;
  return apiRequest<PagedResult<AdminUserSummary>>({ url, requiresAuth: true });
}

// Khóa tài khoản người dùng.
export async function lockUser(userId: string): Promise<AdminUserSummary> {
  return apiRequest<AdminUserSummary>({
    url: `/api/admin/users/${userId}/lock`,
    method: 'POST',
    requiresAuth: true
  });
}

// Mở khóa tài khoản người dùng.
export async function unlockUser(userId: string): Promise<AdminUserSummary> {
  return apiRequest<AdminUserSummary>({
    url: `/api/admin/users/${userId}/unlock`,
    method: 'POST',
    requiresAuth: true
  });
}

// Cập nhật chức danh hiển thị của người dùng.
export async function updateJobPosition(userId: string, jobPosition: string | null): Promise<AdminUserSummary> {
  return apiRequest<AdminUserSummary>({
    url: `/api/admin/users/${userId}/job-position`,
    method: 'PUT',
    data: { jobPosition },
    requiresAuth: true
  });
}
