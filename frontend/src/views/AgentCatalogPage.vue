<script setup lang="ts">
import { Building2, CircleAlert, LoaderCircle, Lock, LogOut, Plus, RefreshCw, Shield, ShieldAlert, ShieldCheck } from '@lucide/vue';
import { computed, onMounted, ref, watch } from 'vue';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import BaseModal from '../components/BaseModal.vue';
import BaseTable from '../components/BaseTable.vue';
import LoginForm from '../components/auth/LoginForm.vue';
import { useAuth } from '../composables/useAuth';
import {
  changePassword,
  createInternalAgent,
  getInternalAgents,
  getTenantAgents,
  getTenants,
  getUsers,
  lockUser,
  unlockUser,
  type AdminUserSummary,
  type AgentListFilters,
  type AgentStatusFilter,
  type AgentSummary,
  type CreateAgentPayload,
  type TenantSummary
} from '../api';
import { ApiError } from '../api/http';
import { formatDate } from '../utils/formatDate';

type AgentScopeView = 'internal' | 'tenant';

interface AvatarOption {
  id: string;
  label: string;
  accent: string;
}

interface StatusOption {
  value: '' | AgentStatusFilter;
  label: string;
}

const avatarOptions: AvatarOption[] = [
  { id: 'mint', label: 'MN', accent: 'linear-gradient(135deg, #63e6be, #12b886)' },
  { id: 'amber', label: 'AM', accent: 'linear-gradient(135deg, #ffd43b, #f08c00)' },
  { id: 'rose', label: 'RS', accent: 'linear-gradient(135deg, #ff8787, #f03e3e)' },
  { id: 'ocean', label: 'OC', accent: 'linear-gradient(135deg, #74c0fc, #1c7ed6)' },
  { id: 'violet', label: 'VT', accent: 'linear-gradient(135deg, #b197fc, #7048e8)' }
];

const statusOptions: StatusOption[] = [
  { value: '', label: 'Tất cả' },
  { value: 'Draft', label: 'Draft' },
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' }
];

const { authState, isAuthenticated, isInitializing, initializeAuth, logout, changePassword: submitPasswordChangeRequest, clearSession } = useAuth();

const activeScope = ref<AgentScopeView>('internal');
const tenants = ref<TenantSummary[]>([]);
const selectedTenantId = ref<string>('');
const internalAgents = ref<AgentSummary[]>([]);
const tenantAgents = ref<AgentSummary[]>([]);

const isLoadingDashboard = ref(false);
const isLoadingTenantAgents = ref(false);
const isSavingInternalAgent = ref(false);
const isLoadingUsers = ref(false);
const activeUserActionId = ref('');
const isPasswordModalOpen = ref(false);
const isChangingPassword = ref(false);

const sidebarError = ref('');
const internalAgentsError = ref('');
const tenantAgentsError = ref('');
const userManagementError = ref('');
const passwordChangeError = ref('');
const authNotice = ref('');

const isCreateModalOpen = ref(false);
const createName = ref('');
const createRole = ref('');
const createDescription = ref('');
const createIcon = ref(avatarOptions[0]?.id ?? 'mint');
const createError = ref('');
const searchText = ref('');
const statusFilter = ref<'' | AgentStatusFilter>('');
const users = ref<AdminUserSummary[]>([]);
const currentPassword = ref('');
const newPassword = ref('');

const selectedTenant = computed(() => tenants.value.find((tenant) => tenant.id === selectedTenantId.value) ?? null);
const hasGlobalWorkspaceAccess = computed(() => !sidebarError.value && !internalAgentsError.value);
const accessTokenExpiresAt = computed(() => authState.value?.accessTokenExpiresAt ?? '');
const hasActiveFilters = computed(() => Boolean(statusFilter.value || searchText.value.trim()));
const activeFilters = computed<AgentListFilters>(() => ({
  status: statusFilter.value,
  search: searchText.value
}));
const emptyStateTitle = computed(() =>
  hasActiveFilters.value
    ? 'Không có agent phù hợp với bộ lọc'
    : activeScope.value === 'internal'
      ? 'Chưa có agent nội bộ'
      : `${selectedTenant.value?.name ?? 'Đơn vị này'} chưa có agent`);
const emptyStateDescription = computed(() => {
  if (hasActiveFilters.value) {
    return 'Hãy thử đổi trạng thái hoặc từ khóa tìm kiếm để xem thêm kết quả.';
  }

  if (activeScope.value === 'internal') {
    return 'Tạo agent đầu tiên để admin có thể dùng riêng trong khu vực nội bộ.';
  }

  return 'Danh sách agent của đơn vị này sẽ xuất hiện ở đây khi có dữ liệu.';
});

onMounted(() => {
  void initializeAuth();
});

watch(
  isAuthenticated,
  (authenticated) => {
    if (authenticated) {
      authNotice.value = '';
      void loadDashboard();
      return;
    }

    resetWorkspace();
  },
  { immediate: true }
);

watch([searchText, statusFilter], (_, __, onCleanup) => {
  if (!isAuthenticated.value) {
    return;
  }

  const timeoutId = window.setTimeout(() => {
    if (activeScope.value === 'internal') {
      void loadInternalAgents();
      return;
    }

    if (selectedTenantId.value) {
      void loadTenantAgents(selectedTenantId.value);
    }
  }, 250);

  onCleanup(() => window.clearTimeout(timeoutId));
});

async function loadDashboard() {
  if (isLoadingDashboard.value) {
    return;
  }

  isLoadingDashboard.value = true;
  sidebarError.value = '';
  internalAgentsError.value = '';
  userManagementError.value = '';

  try {
    const [tenantResult, internalAgentResult, userResult] = await Promise.allSettled([
      getTenants(),
      getInternalAgents(activeFilters.value),
      loadUsers(true)
    ]);

    if (tenantResult.status === 'fulfilled') {
      tenants.value = tenantResult.value;
      if (!selectedTenantId.value && tenants.value.length > 0) {
        selectedTenantId.value = tenants.value[0].id;
      }
    } else {
      tenants.value = [];
      sidebarError.value = normalizeError(tenantResult.reason, 'Không tải được danh sách đơn vị.');
    }

    if (internalAgentResult.status === 'fulfilled') {
      internalAgents.value = internalAgentResult.value;
    } else {
      if (handleAuthFailure(internalAgentResult.reason)) {
        return;
      }

      internalAgents.value = [];
      internalAgentsError.value = normalizeError(internalAgentResult.reason, 'Không tải được danh sách agent nội bộ.');
    }

    if (userResult.status === 'rejected' && !handleAuthFailure(userResult.reason)) {
      userManagementError.value = normalizeError(userResult.reason, 'Không tải được danh sách tài khoản.');
    }

    if (!sidebarError.value && activeScope.value === 'tenant' && selectedTenantId.value) {
      await loadTenantAgents(selectedTenantId.value);
    }
  } finally {
    isLoadingDashboard.value = false;
  }
}

async function refreshDashboard() {
  tenantAgentsError.value = '';
  if (activeScope.value === 'internal') {
    await loadInternalAgents();
    return;
  }

  if (selectedTenantId.value) {
    await loadTenantAgents(selectedTenantId.value);
  }
}

async function loadTenantAgents(tenantId: string) {
  selectedTenantId.value = tenantId;
  activeScope.value = 'tenant';
  tenantAgentsError.value = '';
  isLoadingTenantAgents.value = true;

  try {
    tenantAgents.value = await getTenantAgents(tenantId, activeFilters.value);
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }

    tenantAgents.value = [];
    tenantAgentsError.value = normalizeError(error, 'Không tải được agent của đơn vị đã chọn.');
  } finally {
    isLoadingTenantAgents.value = false;
  }
}

async function loadInternalAgents() {
  internalAgentsError.value = '';
  isLoadingDashboard.value = true;

  try {
    internalAgents.value = await getInternalAgents(activeFilters.value);
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }

    internalAgents.value = [];
    internalAgentsError.value = normalizeError(error, 'Không tải được danh sách agent nội bộ.');
  } finally {
    isLoadingDashboard.value = false;
  }
}

async function showInternalScope() {
  activeScope.value = 'internal';
  await loadInternalAgents();
}

async function loadUsers(silent = false) {
  if (!silent) {
    isLoadingUsers.value = true;
  }

  try {
    users.value = await getUsers();
    userManagementError.value = '';
    return users.value;
  } catch (error) {
    if (handleAuthFailure(error)) {
      return [];
    }

    userManagementError.value = normalizeError(error, 'Không tải được danh sách tài khoản.');
    throw error;
  } finally {
    if (!silent) {
      isLoadingUsers.value = false;
    }
  }
}

function openCreateModal() {
  createError.value = '';
  isCreateModalOpen.value = true;
}

function closeCreateModal(force = false) {
  if (isSavingInternalAgent.value && !force) {
    return;
  }

  isCreateModalOpen.value = false;
  createError.value = '';
  createName.value = '';
  createRole.value = '';
  createDescription.value = '';
  createIcon.value = avatarOptions[0]?.id ?? 'mint';
}

async function submitCreateInternalAgent() {
  createError.value = '';

  if (!createName.value.trim() || !createRole.value.trim()) {
    createError.value = 'Tên và vai trò là bắt buộc.';
    return;
  }

  const payload: CreateAgentPayload = {
    name: createName.value.trim(),
    role: createRole.value.trim(),
    description: createDescription.value.trim() || undefined,
    icon: createIcon.value
  };

  isSavingInternalAgent.value = true;
  try {
    await createInternalAgent(payload);
    activeScope.value = 'internal';
    closeCreateModal(true);
    await loadInternalAgents();
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }

    createError.value = normalizeError(error, 'Không tạo được agent nội bộ.');
  } finally {
    isSavingInternalAgent.value = false;
  }
}

function openPasswordModal() {
  passwordChangeError.value = '';
  currentPassword.value = '';
  newPassword.value = '';
  isPasswordModalOpen.value = true;
}

function closePasswordModal(force = false) {
  if (isChangingPassword.value && !force) {
    return;
  }

  isPasswordModalOpen.value = false;
  passwordChangeError.value = '';
  currentPassword.value = '';
  newPassword.value = '';
}

async function submitPasswordChange() {
  passwordChangeError.value = '';

  if (!currentPassword.value || !newPassword.value) {
    passwordChangeError.value = 'Vui lòng nhập đủ mật khẩu hiện tại và mật khẩu mới.';
    return;
  }

  if (currentPassword.value === newPassword.value) {
    passwordChangeError.value = 'Mật khẩu mới cần khác mật khẩu hiện tại.';
    return;
  }

  isChangingPassword.value = true;
  try {
    await submitPasswordChangeRequest(currentPassword.value, newPassword.value);
    authNotice.value = 'Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại với mật khẩu mới.';
    closePasswordModal(true);
  } catch (error) {
    passwordChangeError.value = normalizeError(error, 'Không thể đổi mật khẩu lúc này.');
  } finally {
    isChangingPassword.value = false;
  }
}

async function toggleUserLock(user: AdminUserSummary) {
  userManagementError.value = '';
  activeUserActionId.value = user.id;

  try {
    const updatedUser = user.status === 'Locked'
      ? await unlockUser(user.id)
      : await lockUser(user.id);

    users.value = users.value.map((item) => (item.id === updatedUser.id ? updatedUser : item));
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }

    userManagementError.value = normalizeError(error, 'Không thể cập nhật trạng thái tài khoản.');
  } finally {
    activeUserActionId.value = '';
  }
}

function resetWorkspace() {
  activeScope.value = 'internal';
  tenants.value = [];
  selectedTenantId.value = '';
  internalAgents.value = [];
  tenantAgents.value = [];
  users.value = [];
  sidebarError.value = '';
  internalAgentsError.value = '';
  tenantAgentsError.value = '';
  userManagementError.value = '';
  closeCreateModal(true);
  closePasswordModal(true);
  searchText.value = '';
  statusFilter.value = '';
}

function normalizeError(error: unknown, fallback: string) {
  if (error instanceof ApiError) {
    if (error.status === 401 && error.body?.code === 'locked_account') {
      return 'Tài khoản đang bị khóa.';
    }

    if (error.status === 401 && error.body?.code === 'session_revoked') {
      return 'Phiên đăng nhập đã bị thu hồi. Vui lòng đăng nhập lại.';
    }

    if (error.status === 403) {
      return 'Tài khoản này chưa có quyền truy cập khu vực quản lý agent.';
    }

    return error.message;
  }

  return fallback;
}

function handleAuthFailure(error: unknown) {
  if (!(error instanceof ApiError) || error.status !== 401) {
    return false;
  }

  authNotice.value = error.body?.code === 'locked_account'
    ? 'Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.'
    : 'Phiên đăng nhập đã hết hiệu lực. Vui lòng đăng nhập lại.';
  clearSession();
  return true;
}

function avatarStyle(icon: string | null | undefined) {
  const option = avatarOptions.find((item) => item.id === icon) ?? avatarOptions[0];
  return { background: option.accent };
}

function avatarLabel(agent: AgentSummary) {
  if (agent.name.trim().length === 0) {
    return 'AI';
  }

  return agent.name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');
}

function statusTone(status: string) {
  if (status === 'Locked') {
    return 'status-chip status-chip--danger';
  }

  if (status === 'Active') {
    return 'status-chip status-chip--success';
  }

  return 'status-chip';
}
</script>

<template>
  <main class="app-shell">
    <p v-if="isInitializing" class="message">Đang kiểm tra phiên đăng nhập...</p>

    <section v-else-if="!isAuthenticated" class="auth-shell">
      <p v-if="authNotice" class="message auth-shell__notice">{{ authNotice }}</p>
      <LoginForm />
    </section>

    <section v-else class="workspace" aria-labelledby="workspace-title">
      <aside class="workspace__sidebar">
        <div class="sidebar__brand">
          <p class="sidebar__eyebrow">Nhân viên AI</p>
          <h1 id="workspace-title">Bảng điều khiển </h1>
          <!-- <p v-if="accessTokenExpiresAt" class="sidebar__meta"> -->
            <!-- Phiên hết hạn: {{ formatDate(accessTokenExpiresAt) }} -->
          <!-- </p> -->
        </div>

        <nav class="sidebar__nav" aria-label="Phạm vi agent">
          <button
            class="scope-link"
            :class="{ 'scope-link--active': activeScope === 'internal' }"
            type="button"
            @click="showInternalScope"
          >
            <Shield :size="17" aria-hidden="true" />
            Nhân viên AI
          </button>
        </nav>

        <section class="tenant-list" aria-labelledby="tenant-list-title">
          <div class="tenant-list__header">
            <h2 id="tenant-list-title">Đơn vị</h2>
            <button class="icon-button" type="button" :disabled="isLoadingDashboard" @click="refreshDashboard">
              <RefreshCw :size="16" :class="{ 'spin': isLoadingDashboard }" aria-hidden="true" />
            </button>
          </div>

          <p v-if="sidebarError" class="message message--error">{{ sidebarError }}</p>
          <p v-else-if="isLoadingDashboard && tenants.length === 0" class="message">Đang tải danh sách đơn vị...</p>
          <div v-else class="tenant-list__items">
            <button
              v-for="tenant in tenants"
              :key="tenant.id"
              class="tenant-link"
              :class="{ 'tenant-link--active': activeScope === 'tenant' && selectedTenantId === tenant.id }"
              type="button"
              @click="loadTenantAgents(tenant.id)"
            >
              <Building2 :size="16" aria-hidden="true" />
              <span>{{ tenant.name }}</span>
            </button>

            <p v-if="tenants.length === 0" class="message">Chưa có đơn vị nào để hiển thị.</p>
          </div>
        </section>

        <BaseButton variant="secondary" type="button" @click="logout">
          <LogOut :size="18" aria-hidden="true" />
          Đăng xuất
        </BaseButton>
      </aside>

      <section class="workspace__content">
        <div v-if="!hasGlobalWorkspaceAccess" class="state-card">
          <CircleAlert :size="28" aria-hidden="true" />
          <div>
            <h2>Không có quyền truy cập</h2>
            <p>{{ internalAgentsError || sidebarError }}</p>
          </div>
        </div>

        <template v-else>
          <header class="content-header">
            <div>
              <p class="content-header__eyebrow">
                {{ activeScope === 'internal' ? 'Khu vực nội bộ' : 'Danh sách theo đơn vị' }}
              </p>
              <h2>
                {{ activeScope === 'internal' ? 'Agent nội bộ' : selectedTenant?.name || 'Chọn một đơn vị' }}
              </h2>
              <p class="content-header__copy">
                {{
                  activeScope === 'internal'
                    ? 'Chỉ quản trị viên được xem và tạo các agent phục vụ nội bộ.'
                    : 'Mỗi đơn vị có một danh sách agent riêng, tách biệt với agent nội bộ.'
                }}
              </p>
            </div>

            <BaseButton
              v-if="activeScope === 'internal'"
              type="button"
              :disabled="Boolean(internalAgentsError)"
              @click="openCreateModal"
            >
              <Plus :size="18" aria-hidden="true" />
              Thêm agent
            </BaseButton>
          </header>

          <section class="security-grid" aria-label="Quản lý bảo mật tài khoản">
            <article class="state-card state-card--compact">
              <div class="state-card__icon">
                <ShieldAlert :size="22" aria-hidden="true" />
              </div>
              <div class="state-card__body">
                <h2>Bảo mật tài khoản</h2>
                <p>Đổi mật khẩu hiện tại để thu hồi toàn bộ phiên cũ và buộc đăng nhập lại.</p>
              </div>
              <BaseButton variant="secondary" type="button" @click="openPasswordModal">
                Đổi mật khẩu
              </BaseButton>
            </article>

            <article class="content-panel user-panel">
              <div class="user-panel__header">
                <div>
                  <p class="content-header__eyebrow">Admin controls</p>
                  <h3>Khóa tài khoản</h3>
                  <p>Khóa tài khoản sẽ chặn đăng nhập mới và thu hồi các phiên còn hoạt động.</p>
                </div>
                <button class="icon-button" type="button" :disabled="isLoadingUsers" @click="loadUsers()">
                  <RefreshCw :size="16" :class="{ 'spin': isLoadingUsers }" aria-hidden="true" />
                </button>
              </div>

              <p v-if="userManagementError" class="message message--error">{{ userManagementError }}</p>
              <div v-else-if="isLoadingUsers && users.length === 0" class="loading-row">
                <LoaderCircle :size="18" class="spin" aria-hidden="true" />
                <span>Đang tải danh sách tài khoản...</span>
              </div>
              <div v-else-if="users.length === 0" class="empty-card empty-card--tight">
                <h3>Chưa có tài khoản</h3>
                <p>Danh sách người dùng quản trị sẽ xuất hiện tại đây khi backend trả dữ liệu.</p>
              </div>
              <BaseTable v-else>
                <thead>
                  <tr>
                    <th>Tài khoản</th>
                    <th>Trạng thái</th>
                    <th>Đổi mật khẩu</th>
                    <th class="table-actions">Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="user in users" :key="user.id">
                    <td>
                      <div class="user-cell">
                        <strong>{{ user.fullName || user.email }}</strong>
                        <span>{{ user.email }}</span>
                      </div>
                    </td>
                    <td>
                      <span :class="statusTone(user.status)">{{ user.status }}</span>
                    </td>
                    <td>{{ user.passwordChangedAt ? formatDate(user.passwordChangedAt) : 'Chưa cập nhật' }}</td>
                    <td class="table-actions">
                      <BaseButton
                        variant="secondary"
                        type="button"
                        :disabled="activeUserActionId === user.id"
                        @click="toggleUserLock(user)"
                      >
                        <component :is="user.status === 'Locked' ? ShieldCheck : Lock" :size="16" aria-hidden="true" />
                        {{ activeUserActionId === user.id ? 'Đang xử lý...' : user.status === 'Locked' ? 'Mở khóa' : 'Khóa' }}
                      </BaseButton>
                    </td>
                  </tr>
                </tbody>
              </BaseTable>
            </article>
          </section>

          <div class="filter-bar">
            <BaseInput v-model="searchText" placeholder="Tìm theo tên, mô tả hoặc vai trò" label="Tìm kiếm agent" />
            <label class="filter-select">
              <span class="sr-only">Lọc theo trạng thái</span>
              <select v-model="statusFilter" aria-label="Lọc theo trạng thái">
                <option v-for="option in statusOptions" :key="option.value || 'all'" :value="option.value">
                  {{ option.label }}
                </option>
              </select>
            </label>
          </div>

          <div v-if="activeScope === 'internal'" class="content-panel">
            <p v-if="internalAgentsError" class="message message--error">{{ internalAgentsError }}</p>
            <div v-else-if="isLoadingDashboard" class="loading-row">
              <LoaderCircle :size="18" class="spin" aria-hidden="true" />
              <span>Đang tải agent nội bộ...</span>
            </div>
            <div v-else-if="internalAgents.length === 0" class="empty-card">
              <h3>{{ emptyStateTitle }}</h3>
              <p>{{ emptyStateDescription }}</p>
            </div>
            <div v-else class="agent-grid">
              <article v-for="agent in internalAgents" :key="agent.id" class="agent-card">
                <div class="agent-card__avatar" :style="avatarStyle(agent.icon)">{{ avatarLabel(agent) }}</div>
                <div class="agent-card__body">
                  <div class="agent-card__top">
                    <div>
                      <h3>{{ agent.name }}</h3>
                      <p>{{ agent.description || 'Agent nội bộ chưa có mô tả.' }}</p>
                    </div>
                    <span class="agent-status">{{ agent.status }}</span>
                  </div>
                  <dl class="agent-meta">
                    <div>
                      <dt>Vai trò</dt>
                      <dd>{{ agent.role }}</dd>
                    </div>
                    <div>
                      <dt>Phạm vi</dt>
                      <dd>{{ agent.scope }}</dd>
                    </div>
                  </dl>
                </div>
              </article>
            </div>
          </div>

          <div v-else class="content-panel">
            <p v-if="tenantAgentsError" class="message message--error">{{ tenantAgentsError }}</p>
            <div v-else-if="isLoadingTenantAgents" class="loading-row">
              <LoaderCircle :size="18" class="spin" aria-hidden="true" />
              <span>Đang tải agent của đơn vị...</span>
            </div>
            <div v-else-if="!selectedTenant" class="empty-card">
              <h3>Chưa chọn đơn vị</h3>
              <p>Chọn một đơn vị ở sidebar để xem các agent dành riêng cho đơn vị đó.</p>
            </div>
            <div v-else-if="tenantAgents.length === 0" class="empty-card">
              <h3>{{ emptyStateTitle }}</h3>
              <p>{{ emptyStateDescription }}</p>
            </div>
            <div v-else class="agent-grid">
              <article v-for="agent in tenantAgents" :key="agent.id" class="agent-card">
                <div class="agent-card__avatar" :style="avatarStyle(agent.icon)">{{ avatarLabel(agent) }}</div>
                <div class="agent-card__body">
                  <div class="agent-card__top">
                    <div>
                      <h3>{{ agent.name }}</h3>
                      <p>{{ agent.description || 'Agent tenant chưa có mô tả.' }}</p>
                    </div>
                    <span class="agent-status">{{ agent.status }}</span>
                  </div>
                  <dl class="agent-meta">
                    <div>
                      <dt>Vai trò</dt>
                      <dd>{{ agent.role }}</dd>
                    </div>
                    <div>
                      <dt>Đơn vị</dt>
                      <dd>{{ selectedTenant.name }}</dd>
                    </div>
                  </dl>
                </div>
              </article>
            </div>
          </div>
        </template>
      </section>
    </section>

    <BaseModal :open="isCreateModalOpen" title="Tạo nhân viên AI" @close="closeCreateModal">
      <div class="create-agent">
        <div class="create-agent__group">
          <p class="create-agent__label">Hình đại diện</p>
          <div class="avatar-picker">
            <button
              v-for="option in avatarOptions"
              :key="option.id"
              class="avatar-choice"
              :class="{ 'avatar-choice--active': createIcon === option.id }"
              :style="{ background: option.accent }"
              type="button"
              @click="createIcon = option.id"
            >
              {{ option.label }}
            </button>
          </div>
        </div>

        <div class="create-agent__group">
          <label class="create-agent__label" for="agent-name">Tên</label>
          <BaseInput id="agent-name" v-model="createName" placeholder="Nhập tên" />
        </div>

        <div class="create-agent__group">
          <label class="create-agent__label" for="agent-role">Vai trò</label>
          <textarea
            id="agent-role"
            v-model="createRole"
            class="agent-textarea"
            rows="3"
            placeholder="Nhập mô tả vai trò (VD: Agent hỗ trợ HR quy trình tuyển dụng)"
          />
        </div>

        <div class="create-agent__group">
          <label class="create-agent__label" for="agent-description">Mô tả</label>
          <textarea
            id="agent-description"
            v-model="createDescription"
            class="agent-textarea"
            rows="4"
            placeholder="Mô tả ngắn về nhiệm vụ hoặc chuyên môn của agent"
          />
        </div>

        <p v-if="createError" class="message message--error">{{ createError }}</p>

        <div class="create-agent__actions">
          <BaseButton variant="secondary" type="button" :disabled="isSavingInternalAgent" @click="closeCreateModal">
            Hủy
          </BaseButton>
          <BaseButton type="button" :disabled="isSavingInternalAgent" @click="submitCreateInternalAgent">
            {{ isSavingInternalAgent ? 'Đang lưu...' : 'Lưu' }}
          </BaseButton>
        </div>
      </div>
    </BaseModal>

    <BaseModal :open="isPasswordModalOpen" title="Đổi mật khẩu" @close="closePasswordModal">
      <form class="create-agent" @submit.prevent="submitPasswordChange">
        <div class="create-agent__group">
          <p class="create-agent__label">Mật khẩu hiện tại</p>
          <BaseInput
            v-model="currentPassword"
            type="password"
            autocomplete="current-password"
            placeholder="Nhập mật khẩu hiện tại"
            label="Mật khẩu hiện tại"
          />
        </div>

        <div class="create-agent__group">
          <p class="create-agent__label">Mật khẩu mới</p>
          <BaseInput
            v-model="newPassword"
            type="password"
            autocomplete="new-password"
            placeholder="Nhập mật khẩu mới"
            label="Mật khẩu mới"
          />
        </div>

        <p v-if="passwordChangeError" class="message message--error">{{ passwordChangeError }}</p>

        <div class="create-agent__actions">
          <BaseButton variant="secondary" type="button" :disabled="isChangingPassword" @click="closePasswordModal">
            Hủy
          </BaseButton>
          <BaseButton type="submit" :disabled="isChangingPassword">
            {{ isChangingPassword ? 'Đang cập nhật...' : 'Xác nhận đổi mật khẩu' }}
          </BaseButton>
        </div>
      </form>
    </BaseModal>
  </main>
</template>
