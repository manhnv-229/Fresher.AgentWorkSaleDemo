<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';
import ComboboxMultiple from '../components/combobox/ComboboxMultiple.vue';
import ComboboxSingle from '../components/combobox/ComboboxSingle.vue';
import PopupTopOneColumn from '../components/popup/PopupTopOneColumn.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import PaginationFooter from '../components/tables/PaginationFooter.vue';
import { getAuditLogs, type AuditLogEntry, type AuditLogFilters } from '../api';
import type { PagedResult } from '../api/agents';
import { ApiError } from '../api/http';
import { formatDate } from '../utils/formatDate';
import { PAGE_SIZE_OPTIONS } from '../composables/useAgentList';
import type { ComboboxOption } from '../components/combobox/Combobox.vue';
import { IconFilter, IconLoader2, IconRefresh } from '@tabler/icons-vue';

const router = useRouter();
const entries = ref<PagedResult<AuditLogEntry>>({ items: [], page: 1, pageSize: PAGE_SIZE_OPTIONS[0], totalCount: 0, totalPages: 0 });
const isLoading = ref(false);
const error = ref('');

const searchText = ref('');
const isMenuOpen = ref(false);
const filterButtonRef = ref<HTMLElement | null>(null);
const filterPopupStyle = ref<Record<string, string>>({});

const selectedTimePreset = ref('');
const selectedActions = ref<string[]>([]);
const selectedTargetType = ref('');
const currentPage = ref(1);
const pageSize = ref<number>(PAGE_SIZE_OPTIONS[0]);
const auditActionOptions = ref<ComboboxOption[]>([]);
const auditTargetOptions = ref<ComboboxOption[]>([]);

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
});

watch(isMenuOpen, async (open) => {
  if (!open) {
    return;
  }

  await nextTick();
  updatePopupPosition();
});

async function loadEntries(filters?: AuditLogFilters) {
  isLoading.value = true;
  error.value = '';
  try {
    const result = await getAuditLogs({
      ...filters,
      page: currentPage.value,
      pageSize: pageSize.value
    });
    if (result.totalPages > 0 && currentPage.value > result.totalPages) {
      currentPage.value = result.totalPages;
      await loadEntries(filters);
      return;
    }

    entries.value = result;
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    error.value = err instanceof ApiError ? err.message : 'Không tải được nhật ký hoạt động.';
  } finally {
    isLoading.value = false;
  }
}

async function loadAuditLogData() {
  await Promise.all([
    loadEntries(),
    loadAuditLogFilterOptions()
  ]);
}

function applySearch() {
  currentPage.value = 1;
  const filters = buildFilters();
  void loadEntries(filters);
}

function applyMenuFilters() {
  currentPage.value = 1;
  const filters = buildFilters();
  void loadEntries(filters);
  closeMenu();
}

function resetMenuFilters() {
  currentPage.value = 1;
  selectedTimePreset.value = '';
  selectedActions.value = [];
  selectedTargetType.value = '';
  const filters = buildFilters();
  void loadEntries(filters);
  closeMenu();
}

watch(pageSize, () => {
  currentPage.value = 1;
  void loadEntries(buildFilters());
});

function goToPage(page: number) {
  currentPage.value = Math.max(1, page);
  void loadEntries(buildFilters());
}

function updatePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize;
}

function buildFilters(): AuditLogFilters | undefined {
  const filters: AuditLogFilters = {};
  if (searchText.value.trim()) filters.search = searchText.value.trim();
  if (selectedTimePreset.value) filters.timePreset = selectedTimePreset.value;
  if (selectedActions.value.length > 0) filters.actions = [...selectedActions.value];
  if (selectedTargetType.value) filters.targetTypes = [selectedTargetType.value];
  return Object.keys(filters).length > 0 ? filters : undefined;
}

function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value;
  if (isMenuOpen.value) {
    void nextTick(() => updatePopupPosition());
  }
}

function closeMenu() {
  isMenuOpen.value = false;
  filterPopupStyle.value = {};
}

function handleWindowResize() {
  if (!isMenuOpen.value) {
    return;
  }

  updatePopupPosition();
}

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

async function loadAuditLogFilterOptions() {
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

function formatAuditLogOptionLabel(value: string) {
  return value
    .split('.').join(' ')
    .split('_').join(' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/^./, (char: string) => char.toUpperCase());
}
</script>

<template>
  <div class="content-panel content-panel--with-pagination">
    <div class="list-toolbar audit-log-toolbar">
      <TextBoxTopLabel
        v-model="searchText"
        label-position="hidden"
        placeholder="Tìm kiếm nhật ký..."
        class="field"
        :disabled="isLoading"
        clearable
        @keydown.enter="applySearch"
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
        <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="loadEntries()">
          <IconRefresh :size="24" :class="{ spin: isLoading }" stroke-width="1.5" aria-hidden="true" />
        </BaseButton>
      </div>
    </div>

    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && entries.items.length === 0" class="loading-row">
      <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
      <span>Đang tải nhật ký hoạt động...</span>
    </div>
    <div v-else-if="entries.items.length === 0" class="empty-card empty-card--tight">
      <h3>Không tìm thấy kết quả</h3>
      <p>{{ hasActiveMenuFilters || searchText ? 'Không có nhật ký nào phù hợp với bộ lọc.' : 'Chưa có nhật ký hoạt động.' }}</p>
    </div>
    <div v-else class="table-shell">
      <table>
      <thead>
        <tr>
          <th>Thời gian</th>
          <th>Người dùng</th>
          <th>Hành động</th>
          <th>Đối tượng</th>
          <th>Mô tả</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="entry in entries.items" :key="entry.id">
          <td>{{ formatDate(entry.createdAt) }}</td>
          <td>{{ entry.userName }}</td>
          <td><span class="status-chip">{{ entry.action }}</span></td>
          <td>{{ entry.targetType || '—' }}</td>
          <td>{{ entry.description }}</td>
        </tr>
      </tbody>
      </table>
    </div>
    <PaginationFooter
      :total-count="entries.totalCount"
      :current-page="currentPage"
      :page-size="pageSize"
      :page-size-options="PAGE_SIZE_OPTIONS"
      count-label="Tổng số"
      @update:currentPage="goToPage"
      @update:pageSize="updatePageSize"
    />
  </div>

  <PopupTopOneColumn
    :open="isMenuOpen"
    title="Bộ lọc nhật ký"
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
        <p class="filter-menu__label">Thời gian</p>
        <select v-model="selectedTimePreset" class="filter-select">
          <option value="">Tất cả</option>
          <option value="today">Hôm nay</option>
          <option value="yesterday">Hôm qua</option>
          <option value="this_week">Tuần này</option>
          <option value="last_week">Tuần trước</option>
          <option value="this_month">Tháng này</option>
          <option value="last_month">Tháng trước</option>
          <option value="this_year">Năm nay</option>
          <option value="last_year">Năm trước</option>
        </select>
      </div>

      <div class="filter-menu__section">
        <p class="filter-menu__label">Hành động</p>
        <ComboboxMultiple
          v-model="selectedActions"
          class="audit-log-filter-popup__combobox"
          label="Hành động"
          label-position="hidden"
          placeholder="Chọn hành động"
          aria-label="Hành động"
          :options="auditActionOptions"
        />
      </div>

      <div class="filter-menu__section">
        <p class="filter-menu__label">Đối tượng</p>
        <ComboboxSingle
          v-model="selectedTargetType"
          class="audit-log-filter-popup__combobox"
          label="Đối tượng"
          label-position="hidden"
          placeholder="Chọn đối tượng"
          aria-label="Đối tượng"
          :options="auditTargetOptions"
        />
      </div>
    </div>

    <template #footer>
      <div class="audit-log-filter-popup__footer-left">
        <BaseButton class="audit-log-filter-popup__reset" variant="secondary" type="button" @click="resetMenuFilters">
          Đặt lại
        </BaseButton>
      </div>

      <div class="audit-log-filter-popup__footer-right">
        <BaseButton variant="secondary" type="button" @click="closeMenu">
          Hủy
        </BaseButton>
        <BaseButton variant="primary" type="button" @click="applyMenuFilters">
          Áp dụng
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
  height: var(--field-height);
  padding: 0 var(--field-padding-x);
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  color: var(--color-text);
  font-size: 13px;
  outline: none;
  cursor: pointer;
}

.filter-select:focus {
  border-color: var(--color-brand);
  box-shadow: 0 0 0 3px rgba(53, 99, 255, 0.12);
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
