import { apiRequest } from './http';
import type { PagedResult } from './agents';

// Một dòng log rút gọn cho màn hình audit log.
export interface AuditLogEntry {
  id: string;
  action: string;
  userName: string;
  createdAt: string;
  targetType: string | null;
  description: string;
}

// Bộ lọc audit log theo text, thời gian và nhóm hành động.
export interface AuditLogFilters {
  search?: string;
  timePreset?: string;
  actions?: string[];
  targetTypes?: string[];
  page?: number;
  pageSize?: number;
}

// Lấy danh sách audit log có phân trang để phục vụ bảng quản trị.
export async function getAuditLogs(filters?: AuditLogFilters): Promise<PagedResult<AuditLogEntry>> {
  const params = new URLSearchParams();
  if (filters?.search) params.set('search', filters.search);
  if (filters?.timePreset) params.set('timePreset', filters.timePreset);
  if (filters?.actions) {
    for (const action of filters.actions) {
      params.append('actions', action);
    }
  }
  if (filters?.targetTypes) {
    for (const targetType of filters.targetTypes) {
      params.append('targetTypes', targetType);
    }
  }
  if (filters?.page) params.set('page', String(filters.page));
  if (filters?.pageSize) params.set('pageSize', String(filters.pageSize));

  const query = params.toString();
  const url = `/api/admin/audit-logs${query ? `?${query}` : ''}`;
  return apiRequest<PagedResult<AuditLogEntry>>({ url, requiresAuth: true });
}
