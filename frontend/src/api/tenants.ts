import { apiRequest } from './http';

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
  return apiRequest<TenantDetail>({ url: `/api/tenants/${tenantId}`, requiresAuth: true });
}

export function createTenant(payload: CreateTenantPayload): Promise<TenantDetail> {
  return apiRequest<TenantDetail, CreateTenantPayload>({
    url: '/api/tenants',
    method: 'POST',
    data: payload,
    requiresAuth: true
  });
}

export function updateTenant(tenantId: string, payload: UpdateTenantPayload): Promise<TenantDetail> {
  return apiRequest<TenantDetail, UpdateTenantPayload>({
    url: `/api/tenants/${tenantId}`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

export function lockTenant(tenantId: string): Promise<TenantDetail> {
  return apiRequest<TenantDetail>({
    url: `/api/tenants/${tenantId}/lock`,
    method: 'POST',
    requiresAuth: true
  });
}
