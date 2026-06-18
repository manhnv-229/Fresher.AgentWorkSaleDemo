import { httpJson } from './http';

export type AgentStatusFilter = 'Draft' | 'Active' | 'Inactive';

export interface TenantSummary {
  id: string;
  name: string;
  code: string;
  status: string;
}

export interface AgentSummary {
  id: string;
  name: string;
  description?: string | null;
  icon?: string | null;
  role: string;
  scope: 'Internal' | 'Tenant';
  status: string;
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
  return httpJson<TenantSummary[]>('/api/tenants', { auth: true });
}

export function getInternalAgents(filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return httpJson<PagedResult<AgentSummary>>(buildAgentListPath('/api/admin/agents/internal', filters), { auth: true });
}

export function getInternalAgentDetail(agentId: string): Promise<AgentDetail> {
  return httpJson<AgentDetail>(`/api/admin/agents/internal/${agentId}`, { auth: true });
}

export function createInternalAgent(payload: CreateAgentPayload): Promise<AgentSummary> {
  return httpJson<AgentSummary, CreateAgentPayload>('/api/admin/agents/internal', {
    method: 'POST',
    body: payload,
    auth: true
  });
}

export function updateInternalAgent(agentId: string, payload: UpdateAgentPayload): Promise<AgentSummary> {
  return httpJson<AgentSummary, UpdateAgentPayload>(`/api/admin/agents/internal/${agentId}`, {
    method: 'PUT',
    body: payload,
    auth: true
  });
}

export function deleteInternalAgent(agentId: string): Promise<void> {
  return httpJson<void>(`/api/admin/agents/internal/${agentId}`, {
    method: 'DELETE',
    auth: true
  });
}

export function getTenantAgents(tenantId: string, filters: AgentListFilters = {}): Promise<PagedResult<AgentSummary>> {
  return httpJson<PagedResult<AgentSummary>>(buildAgentListPath(`/api/tenants/${tenantId}/agents`, filters), { auth: true });
}

export function getTenantAgentDetail(tenantId: string, agentId: string): Promise<AgentDetail> {
  return httpJson<AgentDetail>(`/api/tenants/${tenantId}/agents/${agentId}`, { auth: true });
}

export function createTenantAgent(tenantId: string, payload: CreateAgentPayload): Promise<AgentSummary> {
  return httpJson<AgentSummary, CreateAgentPayload>(`/api/tenants/${tenantId}/agents`, {
    method: 'POST',
    body: payload,
    auth: true
  });
}

export function updateTenantAgent(tenantId: string, agentId: string, payload: UpdateAgentPayload): Promise<AgentSummary> {
  return httpJson<AgentSummary, UpdateAgentPayload>(`/api/tenants/${tenantId}/agents/${agentId}`, {
    method: 'PUT',
    body: payload,
    auth: true
  });
}

export function deleteTenantAgent(tenantId: string, agentId: string): Promise<void> {
  return httpJson<void>(`/api/tenants/${tenantId}/agents/${agentId}`, {
    method: 'DELETE',
    auth: true
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
