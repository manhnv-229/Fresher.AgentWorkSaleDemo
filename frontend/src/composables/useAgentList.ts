import { computed, ref, watch } from 'vue';
import {
  getInternalAgents,
  getTenantAgents,
  type AgentListFilters,
  type AgentStatusFilter,
  type AgentSummary,
  type PagedResult
} from '../api';
import { ApiError } from '../api/http';

export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

type AgentListScope = 'internal' | 'tenant';

interface AgentListCacheEntry {
  items: AgentSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  loadedPages: Set<number>;
}

interface AgentListControllerOptions {
  scope: AgentListScope;
  getTenantId?: () => string;
  fetchPage: (page: number, filters: AgentListFilters) => Promise<PagedResult<AgentSummary>>;
  emptyMessage: string;
}

const agentListCache = new Map<string, AgentListCacheEntry>();

function createEmptyResult(pageSize: number): PagedResult<AgentSummary> {
  return {
    items: [],
    page: 1,
    pageSize,
    totalCount: 0,
    totalPages: 0
  };
}

function normalizeSearch(search: string | undefined) {
  return search?.trim() ?? '';
}

function buildCacheKey(scope: AgentListScope, tenantId: string, filters: AgentListFilters): string {
  return JSON.stringify({
    scope,
    tenantId,
    search: normalizeSearch(filters.search),
    status: filters.status || '',
    pageSize: filters.pageSize || PAGE_SIZE_OPTIONS[0]
  });
}

function cloneEntry(entry: AgentListCacheEntry): AgentListCacheEntry {
  return {
    items: [...entry.items],
    page: entry.page,
    pageSize: entry.pageSize,
    totalCount: entry.totalCount,
    totalPages: entry.totalPages,
    loadedPages: new Set(entry.loadedPages)
  };
}

function mergeResult(
  existing: AgentListCacheEntry | undefined,
  result: PagedResult<AgentSummary>,
  page: number
): AgentListCacheEntry {
  const base = existing ?? {
    items: [],
    page,
    pageSize: result.pageSize,
    totalCount: result.totalCount,
    totalPages: result.totalPages,
    loadedPages: new Set<number>()
  };

  const items = page <= 1 ? [...result.items] : [...base.items, ...result.items];
  const loadedPages = new Set(base.loadedPages);
  loadedPages.add(page);

  return {
    items,
    page,
    pageSize: result.pageSize,
    totalCount: result.totalCount,
    totalPages: result.totalPages,
    loadedPages
  };
}

function filterVisibleAgents(result: PagedResult<AgentSummary>): PagedResult<AgentSummary> {
  const visibleItems = result.items.filter(agent => agent.status !== 'Deleted');
  return {
    ...result,
    items: visibleItems
  };
}

function createAgentListController(options: AgentListControllerOptions, filters: ReturnType<typeof useAgentList>) {
  const agents = ref<PagedResult<AgentSummary>>(createEmptyResult(filters.pageSize.value));
  const isLoading = ref(false);
  const isLoadingMore = ref(false);
  const error = ref('');
  const isReady = ref(false);

  let activeCacheKey = '';
  let requestToken = 0;

  const hasMore = computed(() => agents.value.totalPages > 0 && agents.value.page < agents.value.totalPages);

  function resolveTenantId() {
    return options.getTenantId?.() || '';
  }

  function resolveCacheKey() {
    return buildCacheKey(options.scope, resolveTenantId(), {
      status: filters.statusFilter.value,
      search: filters.searchText.value,
      pageSize: filters.pageSize.value
    });
  }

  function hydrateFromCache(key: string) {
    const cached = agentListCache.get(key);
    if (!cached) {
      agents.value = createEmptyResult(filters.pageSize.value);
      return false;
    }

    const entry = cloneEntry(cached);
    agents.value = {
      items: entry.items,
      page: entry.page,
      pageSize: entry.pageSize,
      totalCount: entry.totalCount,
      totalPages: entry.totalPages
    };
    return true;
  }

  function syncCache(key: string, entry: AgentListCacheEntry) {
    agentListCache.set(key, entry);
    agents.value = {
      items: [...entry.items],
      page: entry.page,
      pageSize: entry.pageSize,
      totalCount: entry.totalCount,
      totalPages: entry.totalPages
    };
  }

  function clearCurrentState() {
    agents.value = createEmptyResult(filters.pageSize.value);
    error.value = '';
    isReady.value = false;
    activeCacheKey = '';
  }

  async function loadPage(page: number, mode: 'replace' | 'append') {
    const cacheKey = resolveCacheKey();
    const cached = agentListCache.get(cacheKey);

    if (mode === 'replace' && activeCacheKey !== cacheKey) {
      activeCacheKey = cacheKey;
      if (hydrateFromCache(cacheKey)) {
        isReady.value = true;
        return;
      }
    }

    if (cached?.loadedPages.has(page)) {
      activeCacheKey = cacheKey;
      hydrateFromCache(cacheKey);
      isReady.value = true;
      return;
    }

    if (mode === 'append' && (isLoading.value || isLoadingMore.value || !hasMore.value)) {
      return;
    }

    const currentToken = ++requestToken;
    error.value = '';
    if (mode === 'replace') {
      isLoading.value = true;
    } else {
      isLoadingMore.value = true;
    }

    try {
      const result = filterVisibleAgents(await options.fetchPage(page, {
        status: filters.statusFilter.value,
        search: filters.searchText.value,
        pageSize: filters.pageSize.value,
        page
      }));

      if (currentToken !== requestToken) {
        return;
      }

      const entry = mergeResult(cached, result, page);
      activeCacheKey = cacheKey;
      syncCache(cacheKey, entry);
      isReady.value = true;
    } catch (err) {
      if (currentToken !== requestToken) {
        return;
      }

      if (err instanceof ApiError && err.status === 401) {
        throw err;
      }

      agents.value = createEmptyResult(filters.pageSize.value);
      error.value = err instanceof ApiError ? err.message : options.emptyMessage;
    } finally {
      if (currentToken === requestToken) {
        isLoading.value = false;
        isLoadingMore.value = false;
      }
    }
  }

  async function loadInitial() {
    clearCurrentState();
    await loadPage(1, 'replace');
  }

  async function loadMore() {
    if (!isReady.value || isLoading.value || isLoadingMore.value || !hasMore.value) {
      return;
    }

    await loadPage(agents.value.page + 1, 'append');
  }

  async function refresh() {
    const cacheKey = resolveCacheKey();
    agentListCache.delete(cacheKey);
    await loadInitial();
  }

  watch(
    () => resolveCacheKey(),
    () => {
      void loadInitial();
    },
    { immediate: true }
  );

  return {
    agents,
    isLoading,
    isLoadingMore,
    error,
    hasMore,
    loadInitial,
    loadMore,
    refresh
  };
}

export function useAgentList() {
  const searchText = ref('');
  const statusFilter = ref<'' | AgentStatusFilter>('');
  const pageSize = ref(PAGE_SIZE_OPTIONS[0]);

  const hasActiveFilters = computed(() => Boolean(statusFilter.value || searchText.value.trim()));
  const activeFilters = computed<AgentListFilters>(() => ({
    status: statusFilter.value,
    search: searchText.value,
    pageSize: pageSize.value
  }));

  return {
    searchText,
    statusFilter,
    pageSize,
    hasActiveFilters,
    activeFilters
  };
}

export function useInternalAgents(filters: ReturnType<typeof useAgentList>) {
  return createAgentListController(
    {
      scope: 'internal',
      fetchPage: (page, listFilters) =>
        getInternalAgents({
          ...listFilters,
          page
        }),
      emptyMessage: 'Không tải được danh sách agent nội bộ.'
    },
    filters
  );
}

export function useTenantAgents(filters: ReturnType<typeof useAgentList>, getTenantId: () => string) {
  return createAgentListController(
    {
      scope: 'tenant',
      getTenantId,
      fetchPage: (page, listFilters) =>
        getTenantAgents(getTenantId(), {
          ...listFilters,
          page
        }),
      emptyMessage: 'Không tải được agent của đơn vị.'
    },
    filters
  );
}
