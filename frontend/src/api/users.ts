import { apiRequest } from './http';
import type { AdminUserSummary } from './auth.types';
import type { PagedResult } from './agents';

export interface MemberListFilters {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

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

export async function lockUser(userId: string): Promise<AdminUserSummary> {
  return apiRequest<AdminUserSummary>({
    url: `/api/admin/users/${userId}/lock`,
    method: 'POST',
    requiresAuth: true
  });
}

export async function unlockUser(userId: string): Promise<AdminUserSummary> {
  return apiRequest<AdminUserSummary>({
    url: `/api/admin/users/${userId}/unlock`,
    method: 'POST',
    requiresAuth: true
  });
}

export async function updateJobPosition(userId: string, jobPosition: string | null): Promise<AdminUserSummary> {
  return apiRequest<AdminUserSummary>({
    url: `/api/admin/users/${userId}/job-position`,
    method: 'PUT',
    data: { jobPosition },
    requiresAuth: true
  });
}
