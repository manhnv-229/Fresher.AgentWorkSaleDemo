import { apiRequest } from './http';

// Thông tin tenant đầy đủ cho màn chi tiết và cấu hình.
export interface TenantDetail {
  id: string;
  name: string;
  code: string;
  status: string;
  createdAt: string;
  modifiedAt?: string | null;
}

// Payload tạo tenant mới.
export interface CreateTenantPayload {
  name: string;
  code: string;
}

// Payload cập nhật tenant hiện có.
export interface UpdateTenantPayload {
  name: string;
  code: string;
}

// Lấy chi tiết tenant theo id.
export function getTenantDetail(tenantId: string): Promise<TenantDetail> {
  return apiRequest<TenantDetail>({ url: `/api/tenants/${tenantId}`, requiresAuth: true });
}

// Tạo tenant mới từ form quản trị.
export function createTenant(payload: CreateTenantPayload): Promise<TenantDetail> {
  return apiRequest<TenantDetail, CreateTenantPayload>({
    url: '/api/tenants',
    method: 'POST',
    data: payload,
    requiresAuth: true
  });
}

// Cập nhật thông tin tenant.
export function updateTenant(tenantId: string, payload: UpdateTenantPayload): Promise<TenantDetail> {
  return apiRequest<TenantDetail, UpdateTenantPayload>({
    url: `/api/tenants/${tenantId}`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

// Khóa tenant để chặn các thao tác chỉnh sửa nghiệp vụ liên quan.
export function lockTenant(tenantId: string): Promise<TenantDetail> {
  return apiRequest<TenantDetail>({
    url: `/api/tenants/${tenantId}/lock`,
    method: 'POST',
    requiresAuth: true
  });
}
