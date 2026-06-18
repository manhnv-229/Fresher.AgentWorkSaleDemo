import { httpJson } from './http';

export interface TenantDetail {
  id: string;
  name: string;
  code: string;
  status: string;
  createdAt: string;
  modifiedAt?: string | null;
}

export interface CreateTenantPayload {
  name: string;
  code: string;
}

export interface UpdateTenantPayload {
  name: string;
  code: string;
}

export function getTenantDetail(tenantId: string): Promise<TenantDetail> {
  return httpJson<TenantDetail>(`/api/tenants/${tenantId}`, { auth: true });
}

export function createTenant(payload: CreateTenantPayload): Promise<TenantDetail> {
  return httpJson<TenantDetail, CreateTenantPayload>('/api/tenants', {
    method: 'POST',
    body: payload,
    auth: true
  });
}

export function updateTenant(tenantId: string, payload: UpdateTenantPayload): Promise<TenantDetail> {
  return httpJson<TenantDetail, UpdateTenantPayload>(`/api/tenants/${tenantId}`, {
    method: 'PUT',
    body: payload,
    auth: true
  });
}

export function lockTenant(tenantId: string): Promise<TenantDetail> {
  return httpJson<TenantDetail>(`/api/tenants/${tenantId}/lock`, {
    method: 'POST',
    auth: true
  });
}
