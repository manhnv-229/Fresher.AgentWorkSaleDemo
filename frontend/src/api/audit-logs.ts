import { httpJson } from './http';

export interface AuditLogEntry {
  id: string;
  action: string;
  userName: string;
  createdAt: string;
  ipAddress: string | null;
  description: string;
}

export interface AuditLogFilters {
  search?: string;
  timePreset?: string;
  actions?: string[];
}

export async function getAuditLogs(filters?: AuditLogFilters): Promise<AuditLogEntry[]> {
  const params = new URLSearchParams();
  if (filters?.search) params.set('search', filters.search);
  if (filters?.timePreset) params.set('timePreset', filters.timePreset);
  if (filters?.actions) {
    for (const action of filters.actions) {
      params.append('actions', action);
    }
  }

  const query = params.toString();
  const url = `/api/admin/audit-logs${query ? `?${query}` : ''}`;
  return httpJson<AuditLogEntry[]>(url, { auth: true });
}
