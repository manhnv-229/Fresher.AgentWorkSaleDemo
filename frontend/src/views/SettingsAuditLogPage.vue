<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseTable from '../components/BaseTable.vue';
import ContentPanel from '../components/ContentPanel.vue';
import ListToolbar from '../components/ListToolbar.vue';
import PaginationFooter from '../components/PaginationFooter.vue';
import { getAuditLogs, type AuditLogEntry, type AuditLogFilters } from '../api';
import type { PagedResult } from '../api/agents';
import { ApiError } from '../api/http';
import { formatDate } from '../utils/formatDate';
import { PAGE_SIZE_OPTIONS } from '../composables/useAgentList';
import { IconFilter, IconLoader2, IconRefresh, IconX } from '@tabler/icons-vue';

const router = useRouter();
const entries = ref<PagedResult<AuditLogEntry>>({ items: [], page: 1, pageSize: PAGE_SIZE_OPTIONS[0], totalCount: 0, totalPages: 0 });
const isLoading = ref(false);
const error = ref('');

const searchText = ref('');
const isMenuOpen = ref(false);
const isActionComboOpen = ref(false);
const isTargetComboOpen = ref(false);
const menuRef = ref<HTMLElement | null>(null);
const actionComboRef = ref<HTMLElement | null>(null);
const targetComboRef = ref<HTMLElement | null>(null);

const TIME_PRESETS = [
  { value: 'today', label: 'Hôm nay' },
  { value: 'yesterday', label: 'Hôm qua' },
  { value: 'this_week', label: 'Tuần này' },
  { value: 'last_week', label: 'Tuần trước' },
  { value: 'this_month', label: 'Tháng này' },
  { value: 'last_month', label: 'Tháng trước' },
  { value: 'this_year', label: 'Năm nay' },
  { value: 'last_year', label: 'Năm trước' }
];

const AVAILABLE_ACTIONS = [
  'login',
  'password_change',
  'user.lock',
  'user.unlock',
  'agent.create',
  'agent.update',
  'agent.delete'
];

const AVAILABLE_TARGET_TYPES = [
  'User',
  'Agent'
];

const selectedTimePreset = ref('');
const selectedActions = ref<string[]>([]);
const selectedTargetTypes = ref<string[]>([]);
const currentPage = ref(1);
const pageSize = ref<number>(PAGE_SIZE_OPTIONS[0]);

const hasActiveMenuFilters = computed(() =>
  selectedTimePreset.value !== '' || selectedActions.value.length > 0 || selectedTargetTypes.value.length > 0
);

const activeFilterCount = computed(() => {
  let count = 0;
  if (selectedTimePreset.value) count++;
  count += selectedActions.value.length;
  count += selectedTargetTypes.value.length;
  return count;
});

onMounted(() => {
  void loadEntries();
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});

function handleClickOutside(e: MouseEvent) {
  const target = e.target as Node;
  if (menuRef.value && !menuRef.value.contains(target)) {
    isMenuOpen.value = false;
    isActionComboOpen.value = false;
    isTargetComboOpen.value = false;
  } else {
    if (actionComboRef.value && !actionComboRef.value.contains(target)) {
      isActionComboOpen.value = false;
    }
    if (targetComboRef.value && !targetComboRef.value.contains(target)) {
      isTargetComboOpen.value = false;
    }
  }
}

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

function applySearch() {
  currentPage.value = 1;
  const filters = buildFilters();
  void loadEntries(filters);
}

function applyMenuFilters() {
  currentPage.value = 1;
  isMenuOpen.value = false;
  isActionComboOpen.value = false;
  isTargetComboOpen.value = false;
  const filters = buildFilters();
  void loadEntries(filters);
}

function resetMenuFilters() {
  currentPage.value = 1;
  selectedTimePreset.value = '';
  selectedActions.value = [];
  selectedTargetTypes.value = [];
  isMenuOpen.value = false;
  isActionComboOpen.value = false;
  isTargetComboOpen.value = false;
  const filters = buildFilters();
  void loadEntries(filters);
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
  if (selectedTargetTypes.value.length > 0) filters.targetTypes = [...selectedTargetTypes.value];
  return Object.keys(filters).length > 0 ? filters : undefined;
}

function toggleAction(action: string) {
  const idx = selectedActions.value.indexOf(action);
  if (idx === -1) {
    selectedActions.value.push(action);
  } else {
    selectedActions.value.splice(idx, 1);
  }
}

function toggleTargetType(targetType: string) {
  const idx = selectedTargetTypes.value.indexOf(targetType);
  if (idx === -1) {
    selectedTargetTypes.value.push(targetType);
  } else {
    selectedTargetTypes.value.splice(idx, 1);
  }
}

function getPresetLabel(value: string): string {
  return TIME_PRESETS.find(p => p.value === value)?.label ?? value;
}

function getActionLabel(value: string): string {
  const labels: Record<string, string> = {
    login: 'Đăng nhập',
    password_change: 'Đổi mật khẩu',
    'user.lock': 'Khóa tài khoản',
    'user.unlock': 'Mở khóa tài khoản',
    'agent.create': 'Tạo agent',
    'agent.update': 'Sửa agent',
    'agent.delete': 'Xóa agent'
  };
  return labels[value] ?? value;
}

function getTargetTypeLabel(value: string): string {
  return value;
}

function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value;
}
</script>

<template>
  <ContentPanel with-pagination>
    <ListToolbar class="audit-log-toolbar">
      <BaseInput
        v-model="searchText"
        placeholder="Tìm kiếm nhật ký..."
        class="field"
        :disabled="isLoading"
        clearable
        @keydown.enter="applySearch"
      />
      <div class="filter-trigger" ref="menuRef">
        <button
          class="filter-button"
          :class="{ 'filter-button--active': hasActiveMenuFilters }"
          type="button"
          :disabled="isLoading"
          @click.stop="toggleMenu"
        >
          <IconFilter :size="20" stroke-width="1.5" aria-hidden="true" />
          <span v-if="activeFilterCount > 0" class="filter-badge">{{ activeFilterCount }}</span>
        </button>

        <div v-if="isMenuOpen" class="filter-menu">
          <div class="filter-menu__section">
            <p class="filter-menu__label">Thời gian</p>
            <select v-model="selectedTimePreset" class="filter-select">
              <option value="">Tất cả</option>
              <option v-for="preset in TIME_PRESETS" :key="preset.value" :value="preset.value">
                {{ preset.label }}
              </option>
            </select>
          </div>

          <div class="filter-menu__divider"></div>

          <div class="filter-menu__section">
            <p class="filter-menu__label">Hành động</p>
            <div class="filter-combo" ref="actionComboRef">
              <button
                class="filter-combo__trigger"
                type="button"
                @click.stop="isActionComboOpen = !isActionComboOpen"
              >
                <span class="filter-combo__value">
                  {{ selectedActions.length === 0 ? 'Tất cả' : `${selectedActions.length} đã chọn` }}
                </span>
                <span class="filter-combo__arrow">▾</span>
              </button>
              <div v-if="isActionComboOpen" class="filter-combo__dropdown">
                <label
                  v-for="action in AVAILABLE_ACTIONS"
                  :key="action"
                  class="filter-combo__option"
                >
                  <input
                    type="checkbox"
                    :checked="selectedActions.includes(action)"
                    @change="toggleAction(action)"
                  />
                  <span>{{ getActionLabel(action) }}</span>
                </label>
              </div>
            </div>
          </div>

          <div class="filter-menu__divider"></div>

          <div class="filter-menu__section">
            <p class="filter-menu__label">Đối tượng</p>
            <div class="filter-combo" ref="targetComboRef">
              <button
                class="filter-combo__trigger"
                type="button"
                @click.stop="isTargetComboOpen = !isTargetComboOpen"
              >
                <span class="filter-combo__value">
                  {{ selectedTargetTypes.length === 0 ? 'Tất cả' : `${selectedTargetTypes.length} đã chọn` }}
                </span>
                <span class="filter-combo__arrow">▾</span>
              </button>
              <div v-if="isTargetComboOpen" class="filter-combo__dropdown">
                <label
                  v-for="targetType in AVAILABLE_TARGET_TYPES"
                  :key="targetType"
                  class="filter-combo__option"
                >
                  <input
                    type="checkbox"
                    :checked="selectedTargetTypes.includes(targetType)"
                    @change="toggleTargetType(targetType)"
                  />
                  <span>{{ getTargetTypeLabel(targetType) }}</span>
                </label>
              </div>
            </div>
          </div>

          <div class="filter-menu__divider"></div>

          <div class="filter-menu__actions">
            <BaseButton variant="primary" type="button" @click="applyMenuFilters">
              Áp dụng
            </BaseButton>
            <BaseButton variant="secondary" type="button" @click="resetMenuFilters">
              <IconX :size="16" stroke-width="1.5" aria-hidden="true" />
              Đặt lại
            </BaseButton>
          </div>
        </div>
      </div>
      <div class="audit-log-toolbar__actions">
        <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="loadEntries()">
          <IconRefresh :size="20" :class="{ spin: isLoading }" stroke-width="1.5" aria-hidden="true" />
        </BaseButton>
      </div>
    </ListToolbar>

    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && entries.items.length === 0" class="loading-row">
      <IconLoader2 :size="20" class="spin" stroke-width="1.5" aria-hidden="true" />
      <span>Đang tải nhật ký hoạt động...</span>
    </div>
    <div v-else-if="entries.items.length === 0" class="empty-card empty-card--tight">
      <h3>Không tìm thấy kết quả</h3>
      <p>{{ hasActiveMenuFilters || searchText ? 'Không có nhật ký nào phù hợp với bộ lọc.' : 'Chưa có nhật ký hoạt động.' }}</p>
    </div>
    <BaseTable v-else>
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
          <td>{{ entry.targetType ? getTargetTypeLabel(entry.targetType) : '—' }}</td>
          <td>{{ entry.description }}</td>
        </tr>
      </tbody>
    </BaseTable>
    <PaginationFooter
      :total-count="entries.totalCount"
      :current-page="currentPage"
      :page-size="pageSize"
      :page-size-options="PAGE_SIZE_OPTIONS"
      count-label="Tổng số"
      @update:currentPage="goToPage"
      @update:pageSize="updatePageSize"
    />
  </ContentPanel>
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

.filter-menu {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  width: 260px;
  padding: 12px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
  z-index: 100;
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

.filter-menu__divider {
  height: 1px;
  background: var(--color-border);
  margin: 0.5rem 0;
}

.filter-menu__actions {
  display: flex;
  gap: 0.5rem;
}

.filter-menu__actions .button {
  flex: 1;
  height: 32px;
  font-size: 13px;
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

.filter-combo {
  position: relative;
}

.filter-combo__trigger {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  height: var(--field-height);
  padding: 0 var(--field-padding-x);
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  color: var(--color-text);
  font-size: 13px;
  cursor: pointer;
}

.filter-combo__trigger:hover {
  background: var(--color-surface-muted);
}

.filter-combo__arrow {
  font-size: 12px;
  color: var(--color-text-subtle);
}

.filter-combo__dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  max-height: 180px;
  overflow-y: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
  z-index: 10;
}

.filter-combo__option {
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 32px;
  padding: 0 12px;
  font-size: 13px;
  color: var(--color-text);
  cursor: pointer;
}

.filter-combo__option:hover {
  background: var(--color-brand-soft);
}

.filter-combo__option input {
  margin: 0;
  accent-color: var(--color-brand);
}

</style>
