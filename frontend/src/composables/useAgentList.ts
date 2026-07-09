import { computed, ref, watch } from 'vue';
import {
  getExternalAgents,
  getInternalAgents,
  getTenantAgents,
  type AgentListFilters,
  type AgentStatusFilter,
  type AgentSummary,
  type PagedResult
} from '../api';
import { ApiError } from '../api/http';

// Các lựa chọn page size dùng chung cho mọi màn danh sách agent.
export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

type AgentListScope = 'internal' | 'tenant' | 'external';

// Bản ghi cache cục bộ cho một tổ hợp scope + filter + pageSize.
interface AgentListCacheEntry {
  items: AgentSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  loadedPages: Set<number>;
}

// Contract tối thiểu để tái sử dụng cùng một controller list cho nhiều scope agent.
interface AgentListControllerOptions {
  scope: AgentListScope;
  getTenantId?: () => string;
  fetchPage: (page: number, filters: AgentListFilters) => Promise<PagedResult<AgentSummary>>;
  emptyMessage: string;
}

const agentListCache = new Map<string, AgentListCacheEntry>();

// Xóa cache list liên quan khi mutation xảy ra từ trang detail hoặc modal CRUD.
export function invalidateAgentListCache(scope: AgentListScope, tenantId = '') {
  for (const key of agentListCache.keys()) {
    try {
      const parsed = JSON.parse(key) as { scope?: AgentListScope; tenantId?: string };
      if (parsed.scope === scope && (scope === 'internal' || parsed.tenantId === tenantId)) {
        agentListCache.delete(key);
      }
    } catch {
      agentListCache.delete(key);
    }
  }
}

// Trả về kết quả rỗng đồng nhất để UI không phải tự dựng fallback object.
function createEmptyResult(pageSize: number): PagedResult<AgentSummary> {
  return {
    items: [],
    page: 1,
    pageSize,
    totalCount: 0,
    totalPages: 0
  };
}

// Chuẩn hóa search text trước khi đưa vào cache key hoặc query string.
function normalizeSearch(search: string | undefined) {
  return search?.trim() ?? '';
}

// Mỗi tổ hợp scope/filter/pageSize có một key riêng trong cache in-memory.
function buildCacheKey(scope: AgentListScope, tenantId: string, filters: AgentListFilters): string {
  return JSON.stringify({
    scope,
    tenantId,
    search: normalizeSearch(filters.search),
    status: filters.status || '',
    pageSize: filters.pageSize || PAGE_SIZE_OPTIONS[0]
  });
}

// Clone entry để tránh view vô tình giữ reference tới Set trong cache gốc.
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

// Gộp kết quả trang mới vào cache hiện có để hỗ trợ infinite scroll.
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

// Frontend bỏ các agent đã soft-delete để UI không phải xử lý riêng ở từng màn.
function filterVisibleAgents(result: PagedResult<AgentSummary>): PagedResult<AgentSummary> {
  const visibleItems = result.items.filter(agent => agent.status !== 'Deleted');
  return {
    ...result,
    items: visibleItems
  };
}

// Factory dùng chung cho internal/tenant/external list, gom cache, load-more và error handling.
function createAgentListController(options: AgentListControllerOptions, filters: ReturnType<typeof useAgentList>) {
  const agents = ref<PagedResult<AgentSummary>>(createEmptyResult(filters.pageSize.value));
  const isLoading = ref(false);
  const isLoadingMore = ref(false);
  const error = ref('');
  const isReady = ref(false);

  let activeCacheKey = '';
  let requestToken = 0;

  const hasMore = computed(() => agents.value.totalPages > 0 && agents.value.page < agents.value.totalPages);

  // Tenant id chỉ áp dụng cho scope tenant; các scope khác trả chuỗi rỗng để key ổn định.
  function resolveTenantId() {
    return options.getTenantId?.() || '';
  }

  // Cache key luôn phản ánh đúng filter đang hiển thị trên UI.
  function resolveCacheKey() {
    return buildCacheKey(options.scope, resolveTenantId(), {
      status: filters.statusFilter.value,
      search: filters.searchText.value,
      pageSize: filters.pageSize.value
    });
  }

  // Ưu tiên render từ cache cục bộ để giảm nhấp nháy khi đổi qua lại giữa các filter hoặc route.
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

  // Đồng bộ cache gốc và reactive state đang bind lên UI.
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

  // Reset state trước một lượt load mới hoàn toàn.
  function clearCurrentState() {
    agents.value = createEmptyResult(filters.pageSize.value);
    error.value = '';
    isReady.value = false;
    activeCacheKey = '';
  }

  // Một token tăng dần được dùng để bỏ kết quả response cũ khi filter đổi nhanh liên tiếp.
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

    // Không gửi request append thừa khi đã ở cuối danh sách hoặc đang load.
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

  // Luôn bắt đầu lại từ trang đầu khi filter hoặc page size thay đổi.
  async function loadInitial() {
    clearCurrentState();
    await loadPage(1, 'replace');
  }

  // Tải thêm trang kế tiếp cho infinite scroll.
  async function loadMore() {
    if (!isReady.value || isLoading.value || isLoadingMore.value || !hasMore.value) {
      return;
    }

    await loadPage(agents.value.page + 1, 'append');
  }

  // Bỏ cache hiện tại rồi tải lại từ API.
  async function refresh() {
    const cacheKey = resolveCacheKey();
    agentListCache.delete(cacheKey);
    await loadInitial();
  }

  // Khi cache key thay đổi nghĩa là filter hiển thị đã đổi, cần tải lại từ đầu.
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

// State filter cơ bản dùng chung cho các màn agent list.
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

// List controller cho agent nội bộ.
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

// List controller cho danh sách external agent trong hub quản trị.
export function useExternalAgents(filters: ReturnType<typeof useAgentList>) {
  return createAgentListController(
    {
      scope: 'external',
      fetchPage: (page, listFilters) =>
        getExternalAgents({
          ...listFilters,
          page
        }),
      emptyMessage: 'Không tải được danh sách agent bên ngoài.'
    },
    filters
  );
}

// List controller cho agent thuộc một tenant cụ thể.
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
