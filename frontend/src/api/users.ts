import { apiRequest } from './http';
import type { AdminUserSummary } from './auth.types';

export interface MemberListFilters {
  search?: string;
  status?: string;
}

export async function getUsers(filters?: MemberListFilters): Promise<AdminUserSummary[]> {
  const params = new URLSearchParams();
  if (filters?.search) params.set('search', filters.search);
  if (filters?.status) params.set('status', filters.status);

  const query = params.toString();
  const url = `/api/admin/users${query ? `?${query}` : ''}`;
  return apiRequest<AdminUserSummary[]>({ url, requiresAuth: true });
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
