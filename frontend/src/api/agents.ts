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

export interface CreateAgentPayload {
  name: string;
  role: string;
  description?: string;
  icon?: string;
}

export interface AgentListFilters {
  status?: AgentStatusFilter | '';
  search?: string;
}

export function getTenants(): Promise<TenantSummary[]> {
  return httpJson<TenantSummary[]>('/api/tenants', { auth: true });
}

export function getInternalAgents(filters: AgentListFilters = {}): Promise<AgentSummary[]> {
  return httpJson<AgentSummary[]>(buildAgentListPath('/api/admin/agents/internal', filters), { auth: true });
}

export function createInternalAgent(payload: CreateAgentPayload): Promise<AgentSummary> {
  return httpJson<AgentSummary, CreateAgentPayload>('/api/admin/agents/internal', {
    method: 'POST',
    body: payload,
    auth: true
  });
}

export function getTenantAgents(tenantId: string, filters: AgentListFilters = {}): Promise<AgentSummary[]> {
  return httpJson<AgentSummary[]>(buildAgentListPath(`/api/tenants/${tenantId}/agents`, filters), { auth: true });
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

  const query = params.toString();
  return query ? `${path}?${query}` : path;
}
