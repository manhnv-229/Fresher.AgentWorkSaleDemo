<script setup lang="ts">
import { Lock, LoaderCircle, RefreshCw, ShieldCheck } from '../icons/tabler';
import { onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseModal from '../components/BaseModal.vue';
import BaseTable from '../components/BaseTable.vue';
import ContentPanel from '../components/ContentPanel.vue';
import ListToolbar from '../components/ListToolbar.vue';
import PaginationFooter from '../components/PaginationFooter.vue';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import { getUsers, lockUser, unlockUser, updateJobPosition, type AdminUserSummary } from '../api';
import type { PagedResult } from '../api/agents';
import type { MemberListFilters } from '../api/users';
import { ApiError } from '../api/http';
import { PAGE_SIZE_OPTIONS } from '../composables/useAgentList';
import { MEMBER_STATUSES, withAllOption, getMemberStatusLabel } from '../utils/statuses';
import { hasMaxLength, isOneOf } from '../utils/validators';

const router = useRouter();
const users = ref<PagedResult<AdminUserSummary>>({ items: [], page: 1, pageSize: PAGE_SIZE_OPTIONS[0], totalCount: 0, totalPages: 0 });
const isLoading = ref(false);
const error = ref('');
const activeActionId = ref('');
const currentPage = ref(1);
const pageSize = ref<number>(PAGE_SIZE_OPTIONS[0]);

const searchText = ref('');
const selectedStatus = ref('');

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
        nextErrors.jobPosition = 'Chức vụ không hợp lệ.';
      } else if (normalizedJobPosition && !hasMaxLength(normalizedJobPosition, 255)) {
        nextErrors.jobPosition = 'Chức vụ không được vượt quá 255 ký tự.';
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

onMounted(() => {
  void loadUsers();
});

watch([searchText, selectedStatus], () => {
  currentPage.value = 1;
  void loadUsers();
});

watch(pageSize, () => {
  currentPage.value = 1;
  void loadUsers();
});

async function loadUsers() {
  isLoading.value = true;
  error.value = '';
  try {
    const filters: MemberListFilters = {
      page: currentPage.value,
      pageSize: pageSize.value
    };
    if (searchText.value.trim()) filters.search = searchText.value.trim();
    if (selectedStatus.value) filters.status = selectedStatus.value;
    const result = await getUsers(filters);
    if (result.totalPages > 0 && currentPage.value > result.totalPages) {
      currentPage.value = result.totalPages;
      await loadUsers();
      return;
    }

    users.value = result;
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      router.push({ name: 'login' });
      return;
    }
    error.value = err instanceof ApiError ? err.message : 'Không tải được danh sách tài khoản.';
  } finally {
    isLoading.value = false;
  }
}

function goToPage(page: number) {
  currentPage.value = Math.max(1, page);
  void loadUsers();
}

function updatePageSize(nextPageSize: number) {
  pageSize.value = nextPageSize;
}

function statusTone(status: string) {
  if (status === 'Locked') return 'status-chip status-chip--danger';
  if (status === 'Active') return 'status-chip status-chip--success';
  return 'status-chip';
}

function openPopup(user: AdminUserSummary) {
  selectedUser.value = user;
  editingJobPosition.value = user.jobPosition || '';
  clearPopupErrors();
  isPopupOpen.value = true;
}

function closePopup() {
  selectedUser.value = null;
  isPopupOpen.value = false;
  clearPopupErrors();
}

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
    setPopupFormError(err instanceof ApiError ? err.message : 'Không cập nhật được trạng thái.');
  } finally {
    activeActionId.value = '';
  }
}

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
    }, 'Không cập nhật được chức vụ.');
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <ContentPanel class="user-panel" with-pagination>
    <ListToolbar class="toolbar">
      <BaseInput
        v-model="searchText"
        placeholder="Tìm kiếm nhân viên..."
        class="toolbar__search"
        :disabled="isLoading"
        clearable
      />
      <select v-model="selectedStatus" class="toolbar__status-filter" :disabled="isLoading">
        <option v-for="option in STATUS_OPTIONS" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>
      <div class="toolbar__actions">
        <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="loadUsers">
          <RefreshCw :size="18" :class="{ spin: isLoading }" aria-hidden="true" />
        </BaseButton>
      </div>
    </ListToolbar>

    <p v-if="error" class="message message--error">{{ error }}</p>
    <div v-else-if="isLoading && users.items.length === 0" class="loading-row">
      <LoaderCircle :size="18" class="spin" aria-hidden="true" />
      <span>Đang tải danh sách tài khoản...</span>
    </div>
    <div v-else-if="users.items.length === 0" class="empty-card empty-card--tight">
      <h3>Không tìm thấy kết quả</h3>
      <p>{{ searchText || selectedStatus ? 'Không có nhân viên nào phù hợp với bộ lọc.' : 'Chưa có tài khoản.' }}</p>
    </div>
    <BaseTable v-else>
      <thead>
        <tr>
          <th>Nhân viên</th>
          <th>Vị trí công việc</th>
          <th>Dự án</th>
          <th>Email</th>
          <th>Trạng thái</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="user in users.items" :key="user.id" class="clickable-row" @click="openPopup(user)">
          <td>
            <div class="user-cell">
              <strong>{{ user.fullName || '—' }}</strong>
              <span>{{ user.employeeCode || '—' }}</span>
            </div>
          </td>
          <td>{{ user.jobPosition || '—' }}</td>
          <td>{{ user.project || '—' }}</td>
          <td>{{ user.email }}</td>
          <td><span :class="statusTone(user.status)">{{ getMemberStatusLabel(user.status) }}</span></td>
        </tr>
      </tbody>
    </BaseTable>
    <PaginationFooter
      :total-count="users.totalCount"
      :current-page="currentPage"
      :page-size="pageSize"
      :page-size-options="PAGE_SIZE_OPTIONS"
      count-label="Tổng số"
      @update:currentPage="goToPage"
      @update:pageSize="updatePageSize"
    />
  </ContentPanel>

  <BaseModal :open="isPopupOpen && Boolean(selectedUser)" title="Thông tin nhân viên" @close="closePopup">
    <div v-if="selectedUser" class="popup">
      <div class="popup__content">
          <p v-if="popupError" class="message message--error">{{ popupError }}</p>

          <div class="popup__section">
            <h4>Thông tin cá nhân</h4>
            <div class="popup__field">
              <label>Họ tên</label>
              <span>{{ selectedUser.fullName || '—' }}</span>
            </div>
            <div class="popup__field">
              <label>Mã nhân viên</label>
              <span>{{ selectedUser.employeeCode || '—' }}</span>
            </div>
            <div class="popup__field">
              <label>Email</label>
              <span>{{ selectedUser.email }}</span>
            </div>
            <div class="popup__field">
              <label>Dự án</label>
              <span>{{ selectedUser.project || '—' }}</span>
            </div>
          </div>

          <div class="popup__section">
            <h4>Chức vụ</h4>
            <div class="popup__field popup__field--edit">
              <select
                v-model="editingJobPosition"
                class="popup-select"
                :class="{ 'popup-select--error': popupErrors.jobPosition }"
                :disabled="isSaving"
                @change="clearPopupFieldError('jobPosition')"
              >
                <option value="">-- Chọn chức vụ --</option>
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
              {{ isSaving ? 'Đang lưu...' : 'Lưu chức vụ' }}
            </BaseButton>
          </div>

          <div class="popup__section">
            <div class="popup__section-header">
              <h4>Trạng thái tài khoản</h4>
              <span :class="statusTone(selectedUser.status)">{{ getMemberStatusLabel(selectedUser.status) }}</span>
            </div>
            <BaseButton
              variant="secondary"
              type="button"
              :disabled="activeActionId === selectedUser.id"
              @click="handleToggleLock"
            >
              <component :is="selectedUser.status === 'Locked' ? ShieldCheck : Lock" :size="16" aria-hidden="true" />
              {{ activeActionId === selectedUser.id ? 'Đang xử lý...' : selectedUser.status === 'Locked' ? 'Mở khóa' : 'Khóa tài khoản' }}
            </BaseButton>
          </div>
      </div>
    </div>
  </BaseModal>
</template>

<style scoped>
.toolbar {
  display: flex;
  gap: var(--table-toolbar-gap);
  align-items: center;
}

.toolbar__search {
  flex: 0 1 360px;
}

.toolbar__status-filter {
  flex: 0 1 240px;
  height: var(--field-height);
  padding: 0 var(--field-padding-x);
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  color: var(--color-text);
  outline: none;
  cursor: pointer;
}

.toolbar__status-filter:focus {
  border-color: var(--color-brand);
  box-shadow: 0 0 0 3px rgba(53, 99, 255, 0.12);
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
