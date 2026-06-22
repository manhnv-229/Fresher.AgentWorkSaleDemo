import { httpJson } from './http';
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
  return httpJson<AdminUserSummary[]>(url, { auth: true });
}

export async function lockUser(userId: string): Promise<AdminUserSummary> {
  return httpJson<AdminUserSummary>(`/api/admin/users/${userId}/lock`, {
    method: 'POST',
    auth: true
  });
}

export async function unlockUser(userId: string): Promise<AdminUserSummary> {
  return httpJson<AdminUserSummary>(`/api/admin/users/${userId}/unlock`, {
    method: 'POST',
    auth: true
  });
}

export async function updateJobPosition(userId: string, jobPosition: string | null): Promise<AdminUserSummary> {
  return httpJson<AdminUserSummary>(`/api/admin/users/${userId}/job-position`, {
    method: 'PUT',
    body: { jobPosition },
    auth: true
  });
}
