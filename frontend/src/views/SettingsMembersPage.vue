<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';
import IconButton from '../components/buttons/IconButton.vue';
import DropdownList from '../components/dropdown/DropdownList.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import DataTable from '../components/tables/DataTable.vue';
import PaginationFooter from '../components/tables/PaginationFooter.vue';
import PrimarySecondaryCell from '../components/tables/PrimarySecondaryCell.vue';
import PopupTopOneColumn from '../components/popup/PopupTopOneColumn.vue';
import type { DataTableColumn, DataTableRow } from '../components/tables/dataTableTypes';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import { getUsers, lockUser, unlockUser, updateJobPosition, type AdminUserSummary } from '../api';
import type { PagedResult } from '../api/agents';
import type { MemberListFilters } from '../api/users';
import { ApiError } from '../api/http';
import { PAGE_SIZE_OPTIONS } from '../composables/useAgentList';
import { MEMBER_STATUSES, withAllOption, getMemberStatusLabel } from '../utils/statuses';
import { hasMaxLength, isOneOf } from '../utils/validators';
import { IconLock, IconLoader2, IconRefresh, IconShieldCheck } from '@tabler/icons-vue';
import { useI18n } from '../i18n';

const router = useRouter();
const { t } = useI18n();
const users = ref<PagedResult<AdminUserSummary>>({ items: [], page: 1, pageSize: PAGE_SIZE_OPTIONS[0], totalCount: 0, totalPages: 0 });
const isLoading = ref(false);
const error = ref('');
const activeActionId = ref('');
const currentPage = ref(1);
const pageSize = ref<number>(PAGE_SIZE_OPTIONS[0]);

const searchText = ref('');
const selectedStatus = ref('');
const searchDebounceMs = 200;
let searchDebounceTimer: number | undefined;
let loadUsersRequestId = 0;

const selectedUser = ref<AdminUserSummary | null>(null);
const isPopupOpen = ref(false);
const editingJobPosition = ref('');
const isSaving = ref(false);
const {
  errors: popupErrors,
  formError: popupError,
  validate: validatePopupForm,
  clearErrors: clearPopupErrors,
  clearFieldError: clearPopupFieldError,
  setFormError: setPopupFormError,
  applyApiError: applyPopupApiError
} = useFormValidation(
  {
    get jobPosition() {
      return editingJobPosition.value;
    }
  },
  [
    (values) => {
      const nextErrors: Partial<Record<'jobPosition', string>> = {};
      const normalizedJobPosition = values.jobPosition.trim();

      if (normalizedJobPosition && !isOneOf(normalizedJobPosition, JOB_POSITIONS)) {
        nextErrors.jobPosition = t('memberPage.errorJobPositionInvalid');
      } else if (normalizedJobPosition && !hasMaxLength(normalizedJobPosition, 255)) {
        nextErrors.jobPosition = t('memberPage.errorJobPositionTooLong');
      }

      return nextErrors;
    }
  ]
);

const JOB_POSITIONS = [
  'Quản trị hệ thống',
  'Quản lý dự án',
  'Nhân viên kỹ thuật',
  'Nhân viên kinh doanh',
  'Nhân viên hành chính',
  'Thực tập sinh'
];

const STATUS_OPTIONS = withAllOption(MEMBER_STATUSES);
const memberTableColumns: DataTableColumn[] = [
  { key: 'fullName', label: t('memberPage.columnName'), minWidth: '220px' },
  { key: 'jobPosition', label: t('memberPage.columnJobPosition'), minWidth: '180px' },
  { key: 'project', label: t('memberPage.columnProject'), minWidth: '180px' },
  { key: 'email', label: t('memberPage.columnEmail'), minWidth: '220px' },
  { key: 'status', label: t('memberPage.columnStatus'), minWidth: '140px' }
];

onMounted(() => {
  void loadUsers();
});

onBeforeUnmount(() => {
  if (searchDebounceTimer !== undefined) {
    window.clearTimeout(searchDebounceTimer);
  }
});

// Search text debounce để tránh gọi API mỗi ký tự khi người dùng nhập nhanh.
watch(searchText, () => {
  // Search chỉ gọi API sau một nhịp ngắn để tránh bắn request liên tục khi gõ nhanh.
  scheduleUserSearch();
});

// Bộ lọc trạng thái thay đổi thì reset pagination và tải lại data ngay.
watch(selectedStatus, () => {
  currentPage.value = 1;
  cancelUserSearchDebounce();
  void loadUsers();
});

// Đổi page size luôn quay về trang đầu để tránh vượt quá tổng trang hiện tại.
watch(pageSize, () => {
  currentPage.value = 1;
  cancelUserSearchDebounce();
  void loadUsers();
});

// Hủy timer cũ trước khi đặt timer mới để chỉ còn request cuối cùng được phát.
function cancelUserSearchDebounce() {
  if (searchDebounceTimer !== undefined) {
    window.clearTimeout(searchDebounceTimer);
    searchDebounceTimer = undefined;
  }
}

// Debounce search member để cân bằng giữa phản hồi UI và số lượng request.
function scheduleUserSearch() {
  cancelUserSearchDebounce();
  searchDebounceTimer = window.setTimeout(() => {
    currentPage.value = 1;
    void loadUsers();
  }, searchDebounceMs);
}

// Tải dữ liệu theo request id mới nhất để bỏ qua response đến chậm.
async function loadUsers(requestId = ++loadUsersRequestId) {
  if (requestId === loadUsersRequestId) {
    isLoading.value = true;
  }
  error.value = '';
  try {
    const filters: MemberListFilters = {
      page: currentPage.value,
      pageSize: pageSize.value
    };
    if (searchText.value.trim()) filters.search = searchText.value.trim();
    if (selectedStatus.value) filters.status = selectedStatus.value;
    const result = await getUsers(filters);
    if (requestId !== loadUsersRequestId) {
      return;
    }
    if (result.totalPages > 0 && currentPage.value > result.totalPages) {
      currentPage.value = result.totalPages;
      await loadUsers(requestId);
      return;
    }

    users.value = result;
  } catch (err) {
    if (requestId !== loadUsersRequestId) {
      return;
    }
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    error.value = err instanceof ApiError ? err.message : t('memberPage.errorLoad');
  } finally {
    if (requestId === loadUsersRequestId) {
      isLoading.value = false;
    }
  }
}

// Chỉ clamp trang hợp lệ rồi dùng chung loader.
function goToPage(page: number) {
  currentPage.value = Math.max(1, page);
  void loadUsers();
}

// Page size đổi theo footer pagination.
function updatePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize;
}

// Tone trạng thái được map ra class để template giữ gọn.
function statusTone(status: string) {
  if (status === 'Locked') return 'status-chip status-chip--danger';
  if (status === 'Active') return 'status-chip status-chip--success';
  return 'status-chip';
}

// Chuẩn hóa dữ liệu undefined/null trước khi render.
function toText(value: unknown): string {
  return value === null || value === undefined ? '' : String(value);
}

function toDisplayValue(value: unknown): string {
  return toText(value) || '—';
}

// Popup detail luôn copy user hiện tại vào state edit trước khi mở.
function openPopup(user: AdminUserSummary) {
  // Popup chỉ mở sau khi copy dữ liệu hiện tại vào form chỉnh sửa.
  selectedUser.value = user;
  editingJobPosition.value = user.jobPosition || '';
  clearPopupErrors();
  isPopupOpen.value = true;
}

// Row click chỉ là lớp bọc để mở popup cùng một logic.
function handleMemberRowClick(row: DataTableRow) {
  openPopup(row as unknown as AdminUserSummary);
}

// Đóng popup và xóa error cũ để lần mở sau sạch state.
function closePopup() {
  selectedUser.value = null;
  isPopupOpen.value = false;
  clearPopupErrors();
}

// Lock/unlock tài khoản là action rời nên refresh lại selected row sau khi xong.
async function handleToggleLock() {
  if (!selectedUser.value) return;
  activeActionId.value = selectedUser.value.id;
  try {
    const updated = selectedUser.value.status === 'Locked'
      ? await unlockUser(selectedUser.value.id)
      : await lockUser(selectedUser.value.id);
    users.value = {
      ...users.value,
      items: users.value.items.map(u => u.id === updated.id ? updated : u)
    };
    selectedUser.value = updated;
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    setPopupFormError(err instanceof ApiError ? err.message : t('memberPage.errorUpdateStatus'));
  } finally {
    activeActionId.value = '';
  }
}

// Lưu chức vụ từ popup detail, sau đó đồng bộ lại row và record đang mở.
async function handleSaveJobPosition() {
  if (!selectedUser.value) return;
  clearPopupErrors();
  if (!validatePopupForm()) {
    return;
  }

  isSaving.value = true;
  try {
    const normalizedJobPosition = editingJobPosition.value.trim();
    const updated = await updateJobPosition(selectedUser.value.id, normalizedJobPosition || null);
    users.value = {
      ...users.value,
      items: users.value.items.map(u => u.id === updated.id ? updated : u)
    };
    selectedUser.value = updated;
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    applyPopupApiError(err, {
      validation_error: FORM_ERROR
    }, t('memberPage.errorUpdateJobPosition'));
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <div class="content-panel content-panel--with-pagination user-panel members-page-panel">
    <div class="list-toolbar toolbar">
      <TextBoxTopLabel
        v-model="searchText"
        label-position="hidden"
        :placeholder="t('memberPage.searchPlaceholder')"
        class="toolbar__search"
        clearable
      />
      <DropdownList
        v-model="selectedStatus"
        class="toolbar__status-filter"
        :label="t('memberPage.filterLabel')"
        label-position="hidden"
        :placeholder="t('memberPage.filterPlaceholder')"
        persistent-placeholder="Trạng thái: "
        :aria-label="t('memberPage.filterLabel')"
        state="normal"
        :options="STATUS_OPTIONS"
        :disabled="isLoading"
      />
      <div class="toolbar__actions">
        <IconButton :ariaLabel="t('memberPage.reload')" :title="t('memberPage.reload')" variant="secondary" type="button" :disabled="isLoading" @click="loadUsers">
          <IconRefresh :size="24" :class="{ spin: isLoading }" stroke-width="1.5" aria-hidden="true" />
        </IconButton>
      </div>
    </div>

    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && users.items.length === 0" class="loading-row">
      <IconLoader2 :size="24" class="spin" stroke-width="1.5" aria-hidden="true" />
      <span>{{ t('memberPage.loading') }}</span>
    </div>
    <div v-else-if="users.items.length === 0" class="empty-card empty-card--tight">
      <h3>{{ t('memberPage.noResultsTitle') }}</h3>
      <p>{{ searchText || selectedStatus ? t('memberPage.noResultsFiltered') : t('memberPage.noResultsEmpty') }}</p>
    </div>
    <DataTable
      v-else
      :columns="memberTableColumns"
      :rows="users.items"
      :show-toolbar="false"
      :show-footer="false"
      :paginate="false"
      :selectable="false"
      row-clickable
      :empty-label="t('memberPage.noResultsEmpty')"
      @row-click="handleMemberRowClick"
    >
      <template #cell-fullName="{ row }">
        <PrimarySecondaryCell :primary="toText(row.fullName)" :secondary="toText(row.employeeCode)" />
      </template>
      <template #cell-jobPosition="{ value }">
        {{ toDisplayValue(value) }}
      </template>
      <template #cell-project="{ value }">
        {{ toDisplayValue(value) }}
      </template>
      <template #cell-email="{ value }">
        {{ toDisplayValue(value) }}
      </template>
      <template #cell-status="{ row }">
        <span :class="statusTone(toText(row.status))">{{ getMemberStatusLabel(toText(row.status)) }}</span>
      </template>
    </DataTable>
    <PaginationFooter
      :total-count="users.totalCount"
      :current-page="currentPage"
      :page-size="pageSize"
      :page-size-options="PAGE_SIZE_OPTIONS"
      :count-label="t('memberPage.totalCount')"
      @update:currentPage="goToPage"
      @update:pageSize="updatePageSize"
    />
  </div>

  <PopupTopOneColumn
    :open="isPopupOpen && Boolean(selectedUser)"
    :title="t('memberPage.title')"
    :cancel-label="t('actions.close')"
    :show-confirm="false"
    @cancel="closePopup"
  >
    <div v-if="selectedUser" class="popup">
      <div class="popup__content">
          <p v-if="popupError" class="message message--error">{{ popupError }}</p>

          <div class="popup__section">
            <h4>{{ t('memberPage.popupPersonalInfo') }}</h4>
            <div class="popup__field">
              <label>{{ t('memberPage.popupFullName') }}</label>
              <span>{{ selectedUser.fullName || '—' }}</span>
            </div>
            <div class="popup__field">
              <label>{{ t('memberPage.popupEmployeeCode') }}</label>
              <span>{{ selectedUser.employeeCode || '—' }}</span>
            </div>
            <div class="popup__field">
              <label>{{ t('memberPage.popupEmail') }}</label>
              <span>{{ selectedUser.email }}</span>
            </div>
            <div class="popup__field">
              <label>{{ t('memberPage.popupProject') }}</label>
              <span>{{ selectedUser.project || '—' }}</span>
            </div>
          </div>

          <div class="popup__section">
            <h4>{{ t('memberPage.popupJobPositionTitle') }}</h4>
            <div class="popup__field popup__field--edit">
              <select
                v-model="editingJobPosition"
                class="popup-select"
                :class="{ 'popup-select--error': popupErrors.jobPosition }"
                :disabled="isSaving"
                @change="clearPopupFieldError('jobPosition')"
              >
                <option value="">{{ t('memberPage.popupChooseJobPosition') }}</option>
                <option v-for="position in JOB_POSITIONS" :key="position" :value="position">
                  {{ position }}
                </option>
              </select>
            </div>
            <p v-if="popupErrors.jobPosition" class="message message--error">{{ popupErrors.jobPosition }}</p>
            <BaseButton
              variant="primary"
              type="button"
              :disabled="isSaving || editingJobPosition === (selectedUser.jobPosition || '')"
              @click="handleSaveJobPosition"
            >
              {{ isSaving ? t('memberPage.popupSaving') : t('memberPage.popupSaveJobPosition') }}
            </BaseButton>
          </div>

          <div class="popup__section">
            <div class="popup__section-header">
              <h4>{{ t('memberPage.popupAccountStatus') }}</h4>
              <span :class="statusTone(selectedUser.status)">{{ getMemberStatusLabel(selectedUser.status) }}</span>
            </div>
            <BaseButton
              variant="secondary"
              type="button"
              :disabled="activeActionId === selectedUser.id"
              @click="handleToggleLock"
            >
              <component :is="selectedUser.status === 'Locked' ? IconShieldCheck : IconLock" :size="24" stroke-width="1.5" aria-hidden="true" />
              {{ activeActionId === selectedUser.id ? t('memberPage.popupProcessing') : selectedUser.status === 'Locked' ? t('memberPage.popupUnlock') : t('memberPage.popupLock') }}
            </BaseButton>
          </div>
      </div>
    </div>
  </PopupTopOneColumn>
</template>

<style scoped>
.toolbar {
  display: flex;
  gap: var(--table-toolbar-gap);
  align-items: center;
}

.members-page-panel {
  padding: 0;
  gap: 0;
}

.members-page-panel .toolbar {
  padding: var(--card-padding) var(--card-padding) 16px;
}

.members-page-panel :deep(.data-table__surface) {
  margin: 0;
}

.toolbar__search {
  flex: 0 1 360px;
}

.toolbar__status-filter {
  flex: 0 1 240px;
  width: 240px;
  min-width: 0;
}

.toolbar__actions {
  margin-left: auto;
}

.clickable-row {
  cursor: pointer;
}

.clickable-row:hover {
  background: var(--color-brand-soft);
}

.popup {
  width: min(100%, 480px);
}

.popup__content {
  display: grid;
  gap: 24px;
  padding: 8px 0 0;
}

.popup__section {
  display: grid;
  gap: 12px;
}

.popup__section h4 {
  margin: 0;
  font-size: var(--font-size-body);
  font-weight: 600;
  color: var(--color-text-subtle);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.popup__field {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.popup__field label {
  font-size: 12px;
  font-weight: 500;
  color: var(--color-text-subtle);
}

.popup__field span {
  color: var(--color-text);
}

.popup__section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.popup__section-header h4 {
  margin: 0;
}

.popup-select {
  width: 100%;
  height: var(--field-height);
  padding: 0 var(--field-padding-x);
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  color: var(--color-text);
  outline: none;
  cursor: pointer;
}

.popup-select:focus {
  border-color: var(--color-brand);
  box-shadow: 0 0 0 3px rgba(53, 99, 255, 0.12);
}

.popup-select--error {
  border-color: var(--color-danger);
}

.popup-select:disabled {
  background: #f9fafb;
  cursor: not-allowed;
}
</style>
