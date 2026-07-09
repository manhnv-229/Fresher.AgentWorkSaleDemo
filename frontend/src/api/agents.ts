import { apiRequest } from './http';

// Trạng thái được dùng ở bộ lọc danh sách agent phía UI.
export type AgentStatusFilter = 'Draft' | 'Active' | 'Published' | 'Inactive';

// Thông tin tenant rút gọn đủ để hiển thị trong dropdown hoặc danh sách chọn ngữ cảnh.
export interface TenantSummary {
  id: string;
  name: string;
  code: string;
  status: string;
}

// Dữ liệu agent cho màn danh sách; một số trường tenant chỉ có ở scope external hoặc tenant.
export interface AgentSummary {
  id: string;
  code?: string;
  name: string;
  description?: string | null;
  icon?: string | null;
  role: string;
  scope: 'Internal' | 'Tenant';
  status: string;
  tenantId?: string | null;
  tenantName?: string | null;
}

// Dữ liệu agent đầy đủ cho màn chi tiết và chỉnh sửa.
export interface AgentDetail {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  icon?: string | null;
  role: string;
  scope: 'Internal' | 'Tenant';
  status: string;
  createdAt: string;
  modifiedAt?: string | null;
  deletedAt?: string | null;
}

// Payload tạo agent mới từ form frontend.
export interface CreateAgentPayload {
  name: string;
  role: string;
  description?: string;
  icon?: string;
}

// Payload cập nhật agent, bao gồm cả trạng thái đích sau thao tác lưu.
export interface UpdateAgentPayload {
  name: string;
  role: string;
  description?: string;
  icon?: string;
  status: string;
}

// Bộ lọc chung cho các màn danh sách agent.
export interface AgentListFilters {
  status?: AgentStatusFilter | '';
  search?: string;
  page?: number;
  pageSize?: number;
}

// Kết quả phân trang dùng lại ở nhiều API list.
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Lấy danh sách tenant để điều hướng sang ngữ cảnh agent theo tenant.
export function getTenants(): Promise<TenantSummary[]> {
  return apiRequest<TenantSummary[]>({ url: '/api/tenants', requiresAuth: true });
}

// Lấy danh sách agent nội bộ cho hub quản trị.
export function getInternalAgents(filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return apiRequest<PagedResult<AgentSummary>>({ url: buildAgentListPath('/api/admin/agents/internal', filters), requiresAuth: true });
}

// Lấy danh sách toàn bộ tenant-agent dưới góc nhìn quản trị tập trung.
export function getExternalAgents(filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return apiRequest<PagedResult<AgentSummary>>({ url: buildAgentListPath('/api/admin/agents/external', filters), requiresAuth: true });
}

// Lấy chi tiết agent nội bộ theo id.
export function getInternalAgentDetail(agentId: string): Promise<AgentDetail> {
  return apiRequest<AgentDetail>({ url: `/api/admin/agents/internal/${agentId}`, requiresAuth: true });
}

// Tạo agent nội bộ mới.
export function createInternalAgent(payload: CreateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, CreateAgentPayload>({
    url: '/api/admin/agents/internal',
    method: 'POST',
    data: payload,
    requiresAuth: true
  });
}

// Cập nhật agent nội bộ hiện có.
export function updateInternalAgent(agentId: string, payload: UpdateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, UpdateAgentPayload>({
    url: `/api/admin/agents/internal/${agentId}`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

// Xóa mềm agent nội bộ.
export function deleteInternalAgent(agentId: string): Promise<void> {
  return apiRequest<void>({
    url: `/api/admin/agents/internal/${agentId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

// Lấy danh sách agent thuộc một tenant cụ thể.
export function getTenantAgents(tenantId: string, filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return apiRequest<PagedResult<AgentSummary>>({ url: buildAgentListPath(`/api/tenants/${tenantId}/agents`, filters), requiresAuth: true });
}

// Lấy chi tiết agent của tenant.
export function getTenantAgentDetail(tenantId: string, agentId: string): Promise<AgentDetail> {
  return apiRequest<AgentDetail>({ url: `/api/tenants/${tenantId}/agents/${agentId}`, requiresAuth: true });
}

// Tạo agent mới trong tenant đang chọn.
export function createTenantAgent(tenantId: string, payload: CreateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, CreateAgentPayload>({
    url: `/api/tenants/${tenantId}/agents`,
    method: 'POST',
    data: payload,
    requiresAuth: true
  });
}

// Cập nhật agent của tenant.
export function updateTenantAgent(tenantId: string, agentId: string, payload: UpdateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, UpdateAgentPayload>({
    url: `/api/tenants/${tenantId}/agents/${agentId}`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

// Xóa mềm agent của tenant.
export function deleteTenantAgent(tenantId: string, agentId: string): Promise<void> {
  return apiRequest<void>({
    url: `/api/tenants/${tenantId}/agents/${agentId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

// Gom logic build query string để các hàm list chỉ cần truyền filter thô từ UI.
function buildAgentListPath(path: string, filters: AgentListFilters): string {
  const params = new URLSearchParams();
  const normalizedSearch = filters.search?.trim();

  if (filters.status) {
    params.set('status', filters.status);
  }

  if (normalizedSearch) {
    params.set('search', normalizedSearch);
  }

  if (filters.page) {
    params.set('page', String(filters.page));
  }

  if (filters.pageSize) {
    params.set('pageSize', String(filters.pageSize));
  }

  const query = params.toString();
  return query ? `${path}?${query}` : path;
}
