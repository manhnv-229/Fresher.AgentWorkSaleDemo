import { httpJson } from './http';
import type { AdminUserSummary } from './auth.types';

export async function getUsers(): Promise<AdminUserSummary[]> {
  return httpJson<AdminUserSummary[]>('/api/admin/users', { auth: true });
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
