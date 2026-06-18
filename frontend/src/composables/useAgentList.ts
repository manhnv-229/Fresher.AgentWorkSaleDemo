import { computed, ref } from 'vue';
import {
  getInternalAgents,
  getTenantAgents,
  type AgentListFilters,
  type AgentStatusFilter,
  type AgentSummary,
  type PagedResult
} from '../api';
import { ApiError } from '../api/http';

export function useAgentList() {
  const searchText = ref('');
  const statusFilter = ref<'' | AgentStatusFilter>('');
  const currentPage = ref(1);
  const pageSize = ref(20);

  const hasActiveFilters = computed(() => Boolean(statusFilter.value || searchText.value.trim()));
  const activeFilters = computed<AgentListFilters>(() => ({
    status: statusFilter.value,
    search: searchText.value
  }));

  return {
    searchText,
    statusFilter,
    currentPage,
    pageSize,
    hasActiveFilters,
    activeFilters
  };
}

export function useInternalAgents(filters: ReturnType<typeof useAgentList>) {
  const agents = ref<PagedResult<AgentSummary>>({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  const isLoading = ref(false);
  const error = ref('');

  async function load() {
    error.value = '';
    isLoading.value = true;
    try {
      agents.value = await getInternalAgents({
        ...filters.activeFilters.value,
        page: filters.currentPage.value,
        pageSize: filters.pageSize.value
      });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        throw err;
      }
      agents.value = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };
      error.value = err instanceof ApiError ? err.message : 'Không tải được danh sách agent nội bộ.';
    } finally {
      isLoading.value = false;
    }
  }

  return { agents, isLoading, error, load };
}

export function useTenantAgents(filters: ReturnType<typeof useAgentList>) {
  const agents = ref<PagedResult<AgentSummary>>({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  const isLoading = ref(false);
  const error = ref('');

  async function load(tenantId: string) {
    error.value = '';
    isLoading.value = true;
    try {
      agents.value = await getTenantAgents(tenantId, {
        ...filters.activeFilters.value,
        page: filters.currentPage.value,
        pageSize: filters.pageSize.value
      });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        throw err;
      }
      agents.value = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };
      error.value = err instanceof ApiError ? err.message : 'Không tải được agent của đơn vị.';
    } finally {
      isLoading.value = false;
    }
  }

  return { agents, isLoading, error, load };
}
