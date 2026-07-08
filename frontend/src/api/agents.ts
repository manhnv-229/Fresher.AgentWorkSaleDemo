import { apiRequest } from './http';

export type AgentStatusFilter = 'Draft' | 'Active' | 'Published' | 'Inactive';

export interface TenantSummary {
  id: string;
  name: string;
  code: string;
  status: string;
}

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

export interface CreateAgentPayload {
  name: string;
  role: string;
  description?: string;
  icon?: string;
}

export interface UpdateAgentPayload {
  name: string;
  role: string;
  description?: string;
  icon?: string;
  status: string;
}

export interface AgentListFilters {
  status?: AgentStatusFilter | '';
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function getTenants(): Promise<TenantSummary[]> {
  return apiRequest<TenantSummary[]>({ url: '/api/tenants', requiresAuth: true });
}

export function getInternalAgents(filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return apiRequest<PagedResult<AgentSummary>>({ url: buildAgentListPath('/api/admin/agents/internal', filters), requiresAuth: true });
}

export function getExternalAgents(filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return apiRequest<PagedResult<AgentSummary>>({ url: buildAgentListPath('/api/admin/agents/external', filters), requiresAuth: true });
}

export function getInternalAgentDetail(agentId: string): Promise<AgentDetail> {
  return apiRequest<AgentDetail>({ url: `/api/admin/agents/internal/${agentId}`, requiresAuth: true });
}

export function createInternalAgent(payload: CreateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, CreateAgentPayload>({
    url: '/api/admin/agents/internal',
    method: 'POST',
    data: payload,
    requiresAuth: true
  });
}

export function updateInternalAgent(agentId: string, payload: UpdateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, UpdateAgentPayload>({
    url: `/api/admin/agents/internal/${agentId}`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

export function deleteInternalAgent(agentId: string): Promise<void> {
  return apiRequest<void>({
    url: `/api/admin/agents/internal/${agentId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

export function getTenantAgents(tenantId: string, filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return apiRequest<PagedResult<AgentSummary>>({ url: buildAgentListPath(`/api/tenants/${tenantId}/agents`, filters), requiresAuth: true });
}

export function getTenantAgentDetail(tenantId: string, agentId: string): Promise<AgentDetail> {
  return apiRequest<AgentDetail>({ url: `/api/tenants/${tenantId}/agents/${agentId}`, requiresAuth: true });
}

export function createTenantAgent(tenantId: string, payload: CreateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, CreateAgentPayload>({
    url: `/api/tenants/${tenantId}/agents`,
    method: 'POST',
    data: payload,
    requiresAuth: true
  });
}

export function updateTenantAgent(tenantId: string, agentId: string, payload: UpdateAgentPayload): Promise<AgentSummary> {
  return apiRequest<AgentSummary, UpdateAgentPayload>({
    url: `/api/tenants/${tenantId}/agents/${agentId}`,
    method: 'PUT',
    data: payload,
    requiresAuth: true
  });
}

export function deleteTenantAgent(tenantId: string, agentId: string): Promise<void> {
  return apiRequest<void>({
    url: `/api/tenants/${tenantId}/agents/${agentId}`,
    method: 'DELETE',
    requiresAuth: true
  });
}

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
