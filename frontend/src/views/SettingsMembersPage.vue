<script setup lang="ts">
import { Lock, LoaderCircle, RefreshCw, ShieldCheck, X } from '@lucide/vue';
import { onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseTable from '../components/BaseTable.vue';
import ContentPanel from '../components/ContentPanel.vue';
import ListToolbar from '../components/ListToolbar.vue';
import PaginationFooter from '../components/PaginationFooter.vue';
import { getUsers, lockUser, unlockUser, updateJobPosition, type AdminUserSummary } from '../api';
import type { PagedResult } from '../api/agents';
import type { MemberListFilters } from '../api/users';
import { ApiError } from '../api/http';
import { PAGE_SIZE_OPTIONS } from '../composables/useAgentList';
import { MEMBER_STATUSES, withAllOption, getMemberStatusLabel } from '../utils/statuses';

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
const popupError = ref('');

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
  popupError.value = '';
  isPopupOpen.value = true;
}

function closePopup() {
  selectedUser.value = null;
  isPopupOpen.value = false;
  popupError.value = '';
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
    popupError.value = err instanceof ApiError ? err.message : 'Không cập nhật được trạng thái.';
  } finally {
    activeActionId.value = '';
  }
}

async function handleSaveJobPosition() {
  if (!selectedUser.value) return;
  isSaving.value = true;
  popupError.value = '';
  try {
    const updated = await updateJobPosition(selectedUser.value.id, editingJobPosition.value || null);
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
    popupError.value = err instanceof ApiError ? err.message : 'Không cập nhật được chức vụ.';
  } finally {
    isSaving.value = false;
  }
}
</script>

<template>
  <div class="settings-content-card">
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
  </div>

  <Teleport to="body">
    <div v-if="isPopupOpen && selectedUser" class="popup-overlay" @click.self="closePopup">
      <div class="popup">
        <div class="popup__header">
          <h3>Thông tin nhân viên</h3>
          <button class="popup__close" type="button" @click="closePopup">
            <X :size="20" aria-hidden="true" />
          </button>
        </div>

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
              <select v-model="editingJobPosition" class="popup-select" :disabled="isSaving">
                <option value="">-- Chọn chức vụ --</option>
                <option v-for="position in JOB_POSITIONS" :key="position" :value="position">
                  {{ position }}
                </option>
              </select>
            </div>
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
    </div>
  </Teleport>
</template>

<style scoped>
.toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
  margin-bottom: 16px;
}

.toolbar__search {
  flex: 0 1 360px;
}

.toolbar__status-filter {
  flex: 0 1 240px;
  height: 40px;
  padding: 0 12px;
  border: 1px solid var(--color-border, #d0d5dd);
  border-radius: 6px;
  background: #fff;
  color: #344054;
  font-size: 14px;
  outline: none;
  cursor: pointer;
}

.toolbar__status-filter:focus {
  border-color: #2479ff;
}

.toolbar__actions {
  margin-left: auto;
}

.clickable-row {
  cursor: pointer;
}

.clickable-row:hover {
  background: #f9fafb;
}

.popup-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.popup {
  background: #fff;
  border-radius: 12px;
  width: 480px;
  max-width: 90vw;
  max-height: 85vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.2);
}

.popup__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 1.25rem;
  border-bottom: 1px solid #e2e8f0;
}

.popup__header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #101828;
}

.popup__close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: #667085;
  cursor: pointer;
  transition: background 120ms ease;
}

.popup__close:hover {
  background: #f2f4f7;
}

.popup__content {
  padding: 1.25rem;
}

.popup__section {
  margin-bottom: 1.5rem;
}

.popup__section:last-child {
  margin-bottom: 0;
}

.popup__section h4 {
  margin: 0 0 0.75rem;
  font-size: 13px;
  font-weight: 600;
  color: #667085;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.popup__field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.75rem;
}

.popup__field label {
  font-size: 12px;
  font-weight: 500;
  color: #667085;
}

.popup__field span {
  font-size: 14px;
  color: #101828;
}

.popup__field--edit {
  margin-bottom: 0.5rem;
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
  height: 40px;
  padding: 0 12px;
  border: 1px solid var(--color-border, #d0d5dd);
  border-radius: 6px;
  background: #fff;
  color: #344054;
  font-size: 14px;
  outline: none;
  cursor: pointer;
}

.popup-select:focus {
  border-color: #2479ff;
}

.popup-select:disabled {
  background: #f9fafb;
  cursor: not-allowed;
}
</style>
