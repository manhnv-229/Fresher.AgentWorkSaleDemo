<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';
import IconButton from '../components/buttons/IconButton.vue';
import ComboboxMultiple from '../components/combobox/ComboboxMultiple.vue';
import ComboboxSingle from '../components/combobox/ComboboxSingle.vue';
import PopupTopOneColumn from '../components/popup/PopupTopOneColumn.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import DataTable from '../components/tables/DataTable.vue';
import PaginationFooter from '../components/tables/PaginationFooter.vue';
import { getAuditLogs, type AuditLogEntry, type AuditLogFilters } from '../api';
import type { PagedResult } from '../api/agents';
import { ApiError } from '../api/http';
import { formatDate } from '../utils/formatDate';
import { PAGE_SIZE_OPTIONS } from '../composables/useAgentList';
import type { ComboboxOption } from '../components/combobox/Combobox.vue';
import type { DataTableColumn } from '../components/tables/dataTableTypes';
import { IconFilter, IconLoader2, IconRefresh } from '@tabler/icons-vue';
import { useI18n } from '../i18n';

const router = useRouter();
const { t } = useI18n();
const entries = ref<PagedResult<AuditLogEntry>>({ items: [], page: 1, pageSize: PAGE_SIZE_OPTIONS[0], totalCount: 0, totalPages: 0 });
const isLoading = ref(false);
const error = ref('');

const searchText = ref('');
const isMenuOpen = ref(false);
const filterButtonRef = ref<HTMLElement | null>(null);
const filterPopupStyle = ref<Record<string, string>>({});
const searchDebounceMs = 200;
let searchDebounceTimer: number | undefined;
let loadEntriesRequestId = 0;

const selectedTimePreset = ref('');
const selectedActions = ref<string[]>([]);
const selectedTargetType = ref('');
const currentPage = ref(1);
const pageSize = ref<number>(PAGE_SIZE_OPTIONS[0]);
const auditActionOptions = ref<ComboboxOption[]>([]);
const auditTargetOptions = ref<ComboboxOption[]>([]);
const auditLogTableColumns: DataTableColumn[] = [
  { key: 'createdAt', label: t('auditLogPage.filterTime'), minWidth: '180px' },
  { key: 'userName', label: t('memberPage.popupEmail'), minWidth: '180px' },
  { key: 'action', label: t('auditLogPage.filterAction'), minWidth: '160px' },
  { key: 'targetType', label: t('auditLogPage.filterTarget'), minWidth: '180px' },
  { key: 'description', label: t('memberPage.popupDescription'), minWidth: '280px', wrap: true }
];
const timePresetOptions = [
  { value: '', label: t('auditLogPage.filterAll') },
  { value: 'today', label: t('auditLogPage.timeToday') },
  { value: 'yesterday', label: t('auditLogPage.timeYesterday') },
  { value: 'this_week', label: t('auditLogPage.timeThisWeek') },
  { value: 'last_week', label: t('auditLogPage.timeLastWeek') },
  { value: 'this_month', label: t('auditLogPage.timeThisMonth') },
  { value: 'last_month', label: t('auditLogPage.timeLastMonth') },
  { value: 'this_year', label: t('auditLogPage.timeThisYear') },
  { value: 'last_year', label: t('auditLogPage.timeLastYear') }
];

const hasActiveMenuFilters = computed(() =>
  selectedTimePreset.value !== '' || selectedActions.value.length > 0 || selectedTargetType.value !== ''
);

const activeFilterCount = computed(() => {
  let count = 0;
  if (selectedTimePreset.value) count++;
  count += selectedActions.value.length;
  if (selectedTargetType.value) count++;
  return count;
});

onMounted(() => {
  void loadAuditLogData();
  window.addEventListener('resize', handleWindowResize);
  window.addEventListener('scroll', handleWindowResize, true);
});

onUnmounted(() => {
  window.removeEventListener('resize', handleWindowResize);
  window.removeEventListener('scroll', handleWindowResize, true);
  cancelSearchDebounce();
});

// Khi popup bộ lọc mở thì đo lại vị trí sau khi DOM update xong.
watch(isMenuOpen, async (open) => {
  if (!open) {
    return;
  }

  await nextTick();
  updatePopupPosition();
});

// Public loader để các nhánh UI có thể gọi lại mà không cần biết request id.
async function loadEntries(filters?: AuditLogFilters) {
  return loadEntriesInternal(filters);
}

// Dùng request id để bỏ qua response cũ khi người dùng đổi filter liên tục.
async function loadEntriesInternal(filters?: AuditLogFilters, requestId = ++loadEntriesRequestId) {
  if (requestId === loadEntriesRequestId) {
    isLoading.value = true;
  }
  error.value = '';
  try {
    const result = await getAuditLogs({
      ...filters,
      page: currentPage.value,
      pageSize: pageSize.value
    });
    if (requestId !== loadEntriesRequestId) {
      return;
    }
    if (result.totalPages > 0 && currentPage.value > result.totalPages) {
      currentPage.value = result.totalPages;
      await loadEntriesInternal(filters, requestId);
      return;
    }

    entries.value = result;
  } catch (err) {
    if (requestId !== loadEntriesRequestId) {
      return;
    }
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    error.value = err instanceof ApiError ? err.message : t('auditLogPage.errorLoad');
  } finally {
    if (requestId === loadEntriesRequestId) {
      isLoading.value = false;
    }
  }
}

// Tải cùng lúc log hiện tại và catalog filter để popup có option đầy đủ.
async function loadAuditLogData() {
  await Promise.all([
    loadEntries(),
    loadAuditLogFilterOptions()
  ]);
}

// Enter trong ô search sẽ chốt page về đầu và nạp theo filter hiện tại.
function applySearch() {
  currentPage.value = 1;
  cancelSearchDebounce();
  const filters = buildFilters();
  void loadEntries(filters);
}

// Chỉ áp dụng filter trong popup sau khi user bấm nút xác nhận.
function applyMenuFilters() {
  currentPage.value = 1;
  cancelSearchDebounce();
  const filters = buildFilters();
  void loadEntries(filters);
  closeMenu();
}

// Reset popup filter đưa toàn bộ state về mặc định rồi load lại dữ liệu.
function resetMenuFilters() {
  currentPage.value = 1;
  selectedTimePreset.value = '';
  selectedActions.value = [];
  selectedTargetType.value = '';
  cancelSearchDebounce();
  const filters = buildFilters();
  void loadEntries(filters);
  closeMenu();
}

// Search audit log debounce để không flood request khi gõ.
watch(searchText, () => {
  // Tìm kiếm nhật ký cũng debounce để tránh gọi API quá dày khi người dùng nhập liên tục.
  scheduleSearch();
});

// Đổi page size thì reset trang để không rơi vào page vượt phạm vi.
watch(pageSize, () => {
  currentPage.value = 1;
  cancelSearchDebounce();
  void loadEntries(buildFilters());
});

// Pagination footer chỉ truyền page mới, còn việc load data nằm ở đây.
function goToPage(page: number) {
  currentPage.value = Math.max(1, page);
  void loadEntries(buildFilters());
}

// Giữ handler riêng để footer update page size rõ ràng hơn.
function updatePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize;
}

// Hủy timer cũ trước khi đặt timer mới.
function cancelSearchDebounce() {
  if (searchDebounceTimer !== undefined) {
    window.clearTimeout(searchDebounceTimer);
    searchDebounceTimer = undefined;
  }
}

// Debounce search ngắn để vẫn phản hồi nhanh nhưng không gọi API quá nhiều.
function scheduleSearch() {
  cancelSearchDebounce();
  searchDebounceTimer = window.setTimeout(() => {
    currentPage.value = 1;
    void loadEntries(buildFilters());
  }, searchDebounceMs);
}

// Gom các filter hiện tại thành payload gọi API.
function buildFilters(): AuditLogFilters | undefined {
  const filters: AuditLogFilters = {};
  if (searchText.value.trim()) filters.search = searchText.value.trim();
  if (selectedTimePreset.value) filters.timePreset = selectedTimePreset.value;
  if (selectedActions.value.length > 0) filters.actions = [...selectedActions.value];
  if (selectedTargetType.value) filters.targetTypes = [selectedTargetType.value];
  return Object.keys(filters).length > 0 ? filters : undefined;
}

// Toggle popup filter và nếu mở thì canh vị trí theo nút bấm.
function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value;
  if (isMenuOpen.value) {
    void nextTick(() => updatePopupPosition());
  }
}

// Đóng popup filter phải dọn cả style vị trí để lần mở sau tính lại từ đầu.
function closeMenu() {
  isMenuOpen.value = false;
  filterPopupStyle.value = {};
}

// Resize/scroll có thể làm lệch popup anchored nên cần cập nhật lại tọa độ.
function handleWindowResize() {
  if (!isMenuOpen.value) {
    return;
  }

  updatePopupPosition();
}

// Tính vị trí popup dựa trên bounding box của nút filter hiện tại.
function updatePopupPosition() {
  const button = filterButtonRef.value;
  if (!button) return;

  const rect = button.getBoundingClientRect();
  const top = Math.round(rect.bottom + 6);
  const left = Math.round(rect.right - 260);

  filterPopupStyle.value = {
    position: 'fixed',
    top: `${top}px`,
    left: `${Math.max(16, left)}px`,
    width: '260px',
    maxWidth: '260px',
    zIndex: '1000'
  };
}

// Quét các trang log để dựng catalog option cho action/targetType.
async function loadAuditLogFilterOptions() {
  // Tải catalog filter bằng cách quét các trang log hiện có rồi gom lại theo action/targetType.
  try {
    const pageSizeForCatalog = 200;
    const actionMap = new Map<string, string>();
    const targetMap = new Map<string, string>();
    let page = 1;
    let totalPages = 1;

    do {
      const result = await getAuditLogs({
        page,
        pageSize: pageSizeForCatalog
      });

      totalPages = result.totalPages || 1;

      for (const entry of result.items) {
        if (entry.action && !actionMap.has(entry.action)) {
          actionMap.set(entry.action, formatAuditLogOptionLabel(entry.action));
        }

        if (entry.targetType && !targetMap.has(entry.targetType)) {
          targetMap.set(entry.targetType, formatAuditLogOptionLabel(entry.targetType));
        }
      }

      page += 1;
    } while (page <= totalPages);

    auditActionOptions.value = [...actionMap.entries()]
      .map(([value, label]) => ({ value, label }))
      .sort((left, right) => left.label.localeCompare(right.label));

    auditTargetOptions.value = [...targetMap.entries()]
      .map(([value, label]) => ({ value, label }))
      .sort((left, right) => left.label.localeCompare(right.label));
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
    }
  }
}

// Chuẩn hóa enum-like value thành label thân thiện để hiển thị trong filter popup.
function formatAuditLogOptionLabel(value: string) {
  return value
    .split('.').join(' ')
    .split('_').join(' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/^./, (char: string) => char.toUpperCase());
}

// Normalize giá trị đầu vào trước khi render hoặc format.
function toText(value: unknown): string {
  return value === null || value === undefined ? '' : String(value);
}

function toDisplayValue(value: unknown): string {
  return toText(value) || '—';
}

function formatAuditDate(value: unknown): string {
  return formatDate(toText(value));
}
</script>

<template>
  <div class="content-panel content-panel--with-pagination audit-log-page-panel">
    <div class="list-toolbar audit-log-toolbar">
      <TextBoxTopLabel
        v-model="searchText"
        label-position="hidden"
        :placeholder="t('auditLogPage.searchPlaceholder')"
        class="field"
        clearable
        @keydown.enter.prevent="applySearch"
      />
      <div class="filter-trigger">
        <button
          ref="filterButtonRef"
          class="filter-button"
          :class="{ 'filter-button--active': hasActiveMenuFilters }"
          type="button"
          :disabled="isLoading"
          @click.stop="toggleMenu"
          >
            <IconFilter :size="24" stroke-width="1.5" aria-hidden="true" />
            <span v-if="activeFilterCount > 0" class="filter-badge">{{ activeFilterCount }}</span>
          </button>
      </div>
      <div class="audit-log-toolbar__actions">
        <IconButton :ariaLabel="t('auditLogPage.reload')" :title="t('auditLogPage.reload')" variant="secondary" type="button" :disabled="isLoading" @click="loadEntries()">
          <IconRefresh :size="24" :class="{ spin: isLoading }" stroke-width="1.5" aria-hidden="true" />
        </IconButton>
      </div>
    </div>

    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && entries.items.length === 0" class="loading-row">
      <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
      <span>{{ t('auditLogPage.loading') }}</span>
    </div>
    <div v-else-if="entries.items.length === 0" class="empty-card empty-card--tight">
      <h3>{{ t('memberPage.noResultsTitle') }}</h3>
      <p>{{ hasActiveMenuFilters || searchText ? t('auditLogPage.noResultsFiltered') : t('auditLogPage.noResultsEmpty') }}</p>
    </div>
    <DataTable
      v-else
      :columns="auditLogTableColumns"
      :rows="entries.items"
      :show-toolbar="false"
      :show-footer="false"
      :paginate="false"
      :selectable="false"
      :empty-label="t('auditLogPage.noResultsEmpty')"
    >
      <template #cell-createdAt="{ row }">
        {{ formatAuditDate(row.createdAt) }}
      </template>
      <template #cell-action="{ value }">
        <span class="status-chip">{{ toDisplayValue(value) }}</span>
      </template>
      <template #cell-targetType="{ value }">
        {{ toDisplayValue(value) }}
      </template>
      <template #cell-description="{ value }">
        {{ toDisplayValue(value) }}
      </template>
    </DataTable>
    <PaginationFooter
      :total-count="entries.totalCount"
      :current-page="currentPage"
      :page-size="pageSize"
      :page-size-options="PAGE_SIZE_OPTIONS"
      :count-label="t('memberPage.totalCount')"
      @update:currentPage="goToPage"
      @update:pageSize="updatePageSize"
    />
  </div>

  <PopupTopOneColumn
    :open="isMenuOpen"
    :title="t('auditLogPage.tableTitle')"
    placement="anchored"
    :panel-style="filterPopupStyle"
    width="480px"
    max-width="480px"
    min-width="480px"
    :show-cancel="false"
    :show-confirm="false"
    @cancel="closeMenu"
    @close="closeMenu"
  >
    <div class="audit-log-filter-popup">
      <div class="filter-menu__section">
        <p class="filter-menu__label">{{ t('auditLogPage.filterTime') }}</p>
        <ComboboxSingle
          v-model="selectedTimePreset"
          class="audit-log-filter-popup__combobox"
          :placeholder="t('auditLogPage.filterAll')"
          :aria-label="t('auditLogPage.filterTime')"
          :options="timePresetOptions"
        />
      </div>

      <div class="filter-menu__section">
        <p class="filter-menu__label">{{ t('auditLogPage.filterAction') }}</p>
        <ComboboxMultiple
          v-model="selectedActions"
          class="audit-log-filter-popup__combobox"
          :label="t('auditLogPage.filterAction')"
          label-position="hidden"
          :placeholder="t('auditLogPage.filterAction')"
          :aria-label="t('auditLogPage.filterAction')"
          :options="auditActionOptions"
        />
      </div>

      <div class="filter-menu__section">
        <p class="filter-menu__label">{{ t('auditLogPage.filterTarget') }}</p>
        <ComboboxSingle
          v-model="selectedTargetType"
          class="audit-log-filter-popup__combobox"
          :label="t('auditLogPage.filterTarget')"
          label-position="hidden"
          :placeholder="t('auditLogPage.filterTarget')"
          :aria-label="t('auditLogPage.filterTarget')"
          :options="auditTargetOptions"
        />
      </div>
    </div>

    <template #footer>
      <div class="audit-log-filter-popup__footer-left">
        <BaseButton class="audit-log-filter-popup__reset" variant="secondary" type="button" @click="resetMenuFilters">
          {{ t('auditLogPage.reset') }}
        </BaseButton>
      </div>

      <div class="audit-log-filter-popup__footer-right">
        <BaseButton variant="secondary" type="button" @click="closeMenu">
          {{ t('auditLogPage.cancel') }}
        </BaseButton>
        <BaseButton variant="primary" type="button" @click="applyMenuFilters">
          {{ t('auditLogPage.apply') }}
        </BaseButton>
      </div>
    </template>
  </PopupTopOneColumn>
</template>

<style scoped>
.audit-log-toolbar {
  display: flex;
  gap: var(--table-toolbar-gap);
  align-items: center;
}

.audit-log-page-panel {
  padding: 0;
  gap: 0;
}

.audit-log-page-panel .audit-log-toolbar {
  padding: var(--card-padding) var(--card-padding) 16px;
}

.audit-log-page-panel :deep(.data-table__surface) {
  margin: 0;
}

.audit-log-toolbar .field {
  flex: 0 1 360px;
}

.audit-log-toolbar__actions {
  margin-left: auto;
}

.filter-trigger {
  position: relative;
}

.filter-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: var(--button-icon-only-size);
  height: var(--button-icon-only-size);
  border: 1px solid var(--color-border);
  border-radius: var(--button-radius);
  background: var(--color-surface);
  color: var(--color-text-subtle);
  cursor: pointer;
  position: relative;
  transition:
    border-color 120ms ease,
    background 120ms ease;
}

.filter-button:hover {
  background: var(--color-surface-muted);
}

.filter-button--active {
  border-color: var(--color-brand);
  background: var(--color-brand-soft);
  color: var(--color-brand);
}

.filter-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: #2479ff;
  color: #fff;
  font-size: 11px;
  font-weight: 600;
  line-height: 16px;
  text-align: center;
}

.filter-menu__section {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.filter-menu__label {
  font-size: 12px;
  font-weight: 600;
  color: var(--color-text-subtle);
  text-transform: uppercase;
  letter-spacing: 0.03em;
  margin-bottom: 0.25rem;
}

.filter-select {
  width: 100%;
}

.audit-log-filter-popup {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}

.audit-log-filter-popup__combobox {
  width: 100%;
}

.audit-log-filter-popup__footer-left {
  display: flex;
  justify-content: flex-start;
}

.audit-log-filter-popup__footer-right {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.audit-log-filter-popup__reset {
  border-color: var(--color-brand);
  background: var(--color-surface);
  color: var(--color-brand);
}

.audit-log-filter-popup__reset:hover {
  border-color: var(--color-brand);
  background: var(--color-brand-soft);
  color: var(--color-brand);
}

</style>
