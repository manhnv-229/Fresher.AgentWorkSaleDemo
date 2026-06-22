<script setup lang="ts">
import { Filter, LoaderCircle, RefreshCw, X } from '@lucide/vue';
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseTable from '../components/BaseTable.vue';
import { getAuditLogs, type AuditLogEntry, type AuditLogFilters } from '../api';
import { ApiError } from '../api/http';
import { formatDate } from '../utils/formatDate';

const router = useRouter();
const entries = ref<AuditLogEntry[]>([]);
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
    entries.value = await getAuditLogs(filters);
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
  const filters = buildFilters();
  void loadEntries(filters);
}

function applyMenuFilters() {
  isMenuOpen.value = false;
  isActionComboOpen.value = false;
  isTargetComboOpen.value = false;
  const filters = buildFilters();
  void loadEntries(filters);
}

function resetMenuFilters() {
  selectedTimePreset.value = '';
  selectedActions.value = [];
  selectedTargetTypes.value = [];
  isMenuOpen.value = false;
  isActionComboOpen.value = false;
  isTargetComboOpen.value = false;
  const filters = buildFilters();
  void loadEntries(filters);
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
  <header class="content-header">
    <div>
      <p class="content-header__eyebrow">Thiết lập</p>
      <h2>Nhật ký hoạt động</h2>
      <p class="content-header__copy">Xem lịch sử thao tác hệ thống</p>
    </div>
    <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="loadEntries()">
      <RefreshCw :size="18" :class="{ spin: isLoading }" aria-hidden="true" />
    </BaseButton>
  </header>

  <div class="content-panel">
    <div class="audit-log-toolbar">
      <BaseInput
        v-model="searchText"
        placeholder="Tìm kiếm nhật ký..."
        class="field"
        :disabled="isLoading"
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
          <Filter :size="18" aria-hidden="true" />
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
              <X :size="14" aria-hidden="true" />
              Đặt lại
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && entries.length === 0" class="loading-row">
      <LoaderCircle :size="18" class="spin" aria-hidden="true" />
      <span>Đang tải nhật ký hoạt động...</span>
    </div>
    <div v-else-if="entries.length === 0" class="empty-card empty-card--tight">
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
        <tr v-for="entry in entries" :key="entry.id">
          <td>{{ formatDate(entry.createdAt) }}</td>
          <td>{{ entry.userName }}</td>
          <td><span class="status-chip">{{ entry.action }}</span></td>
          <td>{{ entry.targetType ? getTargetTypeLabel(entry.targetType) : '—' }}</td>
          <td>{{ entry.description }}</td>
        </tr>
      </tbody>
    </BaseTable>
  </div>
</template>

<style scoped>
.audit-log-toolbar {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  margin-bottom: 1rem;
}

.audit-log-toolbar .field {
  flex: 0 1 360px;
}

.filter-trigger {
  position: relative;
}

.filter-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: 1px solid var(--color-border, #d0d5dd);
  border-radius: var(--radius-md, 6px);
  background: var(--color-surface, #fff);
  color: #344054;
  cursor: pointer;
  position: relative;
  transition: border-color 120ms ease, background 120ms ease;
}

.filter-button:hover {
  background: #f9fafb;
}

.filter-button--active {
  border-color: #2479ff;
  background: #eff6ff;
  color: #2479ff;
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
  padding: 0.75rem;
  border: 1px solid var(--color-border, #e2e8f0);
  border-radius: 8px;
  background: #fff;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
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
  color: #667085;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  margin-bottom: 0.25rem;
}

.filter-menu__divider {
  height: 1px;
  background: #e2e8f0;
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
  height: 34px;
  padding: 0 8px;
  border: 1px solid var(--color-border, #d0d5dd);
  border-radius: 6px;
  background: #fff;
  color: #344054;
  font-size: 13px;
  outline: none;
  cursor: pointer;
}

.filter-select:focus {
  border-color: #2479ff;
}

.filter-combo {
  position: relative;
}

.filter-combo__trigger {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  height: 34px;
  padding: 0 8px;
  border: 1px solid var(--color-border, #d0d5dd);
  border-radius: 6px;
  background: #fff;
  color: #344054;
  font-size: 13px;
  cursor: pointer;
}

.filter-combo__trigger:hover {
  background: #f9fafb;
}

.filter-combo__arrow {
  font-size: 12px;
  color: #667085;
}

.filter-combo__dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  max-height: 180px;
  overflow-y: auto;
  border: 1px solid var(--color-border, #e2e8f0);
  border-radius: 6px;
  background: #fff;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  z-index: 10;
}

.filter-combo__option {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.4rem 0.6rem;
  font-size: 13px;
  color: #344054;
  cursor: pointer;
}

.filter-combo__option:hover {
  background: #f9fafb;
}

.filter-combo__option input {
  margin: 0;
  accent-color: #2479ff;
}
</style>
