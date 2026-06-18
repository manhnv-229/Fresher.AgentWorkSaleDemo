<script setup lang="ts">
import { Building2, CircleAlert, LoaderCircle, Lock, LogOut, Plus, RefreshCw, Settings2, Shield, ShieldCheck } from '@lucide/vue';
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
  deleteInternalAgent,
  deleteTenantAgent,
  getInternalAgentDetail,
  getInternalAgents,
  getTenantAgentDetail,
  getTenantAgents,
  getTenants,
  getUsers,
  lockUser,
  unlockUser,
  updateInternalAgent,
  updateTenantAgent,
  type AdminUserSummary,
  type AgentDetail,
  type AgentListFilters,
  type AgentStatusFilter,
  type AgentSummary,
  type CreateAgentPayload,
  type PagedResult,
  type TenantSummary,
  type UpdateAgentPayload
} from '../api';
import { ApiError } from '../api/http';
import { formatDate } from '../utils/formatDate';

type AgentScopeView = 'internal' | 'tenant';
type WorkspaceView = AgentScopeView | 'settings' | 'detail';
type SettingsSection = 'members' | 'password';
type ViewMode = 'list' | 'detail' | 'edit';

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

const allStatusOptions: StatusOption[] = [
  { value: 'Draft', label: 'Draft' },
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' }
];

const { authState, isAuthenticated, isInitializing, initializeAuth, logout, changePassword: submitPasswordChangeRequest, clearSession } = useAuth();

const activeWorkspace = ref<WorkspaceView>('internal');
const activeSettingsSection = ref<SettingsSection>('members');
const viewMode = ref<ViewMode>('list');
const tenants = ref<TenantSummary[]>([]);
const selectedTenantId = ref<string>('');
const internalAgents = ref<PagedResult<AgentSummary>>({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
const tenantAgents = ref<PagedResult<AgentSummary>>({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
const currentPage = ref(1);
const pageSize = ref(20);

const isLoadingDashboard = ref(false);
const isLoadingTenantAgents = ref(false);
const isSavingInternalAgent = ref(false);
const isLoadingUsers = ref(false);
const activeUserActionId = ref('');
const isChangingPassword = ref(false);
const isLoadingAgentDetail = ref(false);
const isSavingAgent = ref(false);
const isDeletingAgent = ref(false);

const sidebarError = ref('');
const internalAgentsError = ref('');
const tenantAgentsError = ref('');
const userManagementError = ref('');
const passwordChangeError = ref('');
const authNotice = ref('');
const agentDetailError = ref('');

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

const selectedAgent = ref<AgentDetail | null>(null);
const editName = ref('');
const editRole = ref('');
const editDescription = ref('');
const editIcon = ref(avatarOptions[0]?.id ?? 'mint');
const editStatus = ref('Draft');
const editError = ref('');

const isDeleteConfirmModalOpen = ref(false);
const agentToDelete = ref<AgentSummary | null>(null);

const selectedTenant = computed(() => tenants.value.find((tenant) => tenant.id === selectedTenantId.value) ?? null);
const hasGlobalWorkspaceAccess = computed(() => (isSettingsWorkspace.value ? true : !sidebarError.value));
const isSettingsWorkspace = computed(() => activeWorkspace.value === 'settings');
const accessTokenExpiresAt = computed(() => authState.value?.accessTokenExpiresAt ?? '');
const hasActiveFilters = computed(() => Boolean(statusFilter.value || searchText.value.trim()));
const activeFilters = computed<AgentListFilters>(() => ({
  status: statusFilter.value,
  search: searchText.value
}));
const workspaceEyebrow = computed(() => (isSettingsWorkspace.value ? 'Thiết lập' : activeWorkspace.value === 'internal' ? 'Khu vực nội bộ' : 'Danh sách theo đơn vị'));
const workspaceTitle = computed(() =>
  isSettingsWorkspace.value
    ? 'Thiết lập'
    : activeWorkspace.value === 'internal'
      ? 'Agent nội bộ'
      : selectedTenant.value?.name || 'Chọn một đơn vị'
);
const settingsSectionTitle = computed(() => (activeSettingsSection.value === 'members' ? 'Quản lý thành viên' : 'Đổi mật khẩu'));
const emptyStateTitle = computed(() =>
  hasActiveFilters.value
    ? 'Không có agent phù hợp với bộ lọc'
    : activeWorkspace.value === 'internal'
      ? 'Chưa có agent nội bộ'
      : `${selectedTenant.value?.name ?? 'Đơn vị này'} chưa có agent`);
const emptyStateDescription = computed(() => {
  if (hasActiveFilters.value) {
    return 'Hãy thử đổi trạng thái hoặc từ khóa tìm kiếm để xem thêm kết quả.';
  }

  if (activeWorkspace.value === 'internal') {
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
    if (activeWorkspace.value === 'internal') {
      void loadInternalAgents();
      return;
    }

    if (activeWorkspace.value === 'tenant' && selectedTenantId.value) {
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

      internalAgents.value = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };
      internalAgentsError.value = normalizeError(internalAgentResult.reason, 'Không tải được danh sách agent nội bộ.');
    }

    if (userResult.status === 'rejected' && !handleAuthFailure(userResult.reason)) {
      userManagementError.value = normalizeError(userResult.reason, 'Không tải được danh sách tài khoản.');
    }

    if (!sidebarError.value && activeWorkspace.value === 'tenant' && selectedTenantId.value) {
      await loadTenantAgents(selectedTenantId.value);
    }
  } finally {
    isLoadingDashboard.value = false;
  }
}

async function refreshDashboard() {
  tenantAgentsError.value = '';
  if (activeWorkspace.value === 'internal') {
    await loadInternalAgents();
    return;
  }

  if (activeWorkspace.value === 'tenant' && selectedTenantId.value) {
    await loadTenantAgents(selectedTenantId.value);
  }
}

async function loadTenantAgents(tenantId: string) {
  selectedTenantId.value = tenantId;
  activeWorkspace.value = 'tenant';
  viewMode.value = 'list';
  tenantAgentsError.value = '';
  isLoadingTenantAgents.value = true;

  try {
    const filters: AgentListFilters = {
      ...activeFilters.value,
      page: currentPage.value,
      pageSize: pageSize.value
    };
    tenantAgents.value = await getTenantAgents(tenantId, filters);
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }

    tenantAgents.value = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } as PagedResult<AgentSummary>;
    tenantAgentsError.value = normalizeError(error, 'Không tải được agent của đơn vị đã chọn.');
  } finally {
    isLoadingTenantAgents.value = false;
  }
}

async function loadInternalAgents() {
  internalAgentsError.value = '';
  isLoadingDashboard.value = true;

  try {
    const filters: AgentListFilters = {
      ...activeFilters.value,
      page: currentPage.value,
      pageSize: pageSize.value
    };
    internalAgents.value = await getInternalAgents(filters);
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }

    internalAgents.value = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } as PagedResult<AgentSummary>;
    internalAgentsError.value = normalizeError(error, 'Không tải được danh sách agent nội bộ.');
  } finally {
    isLoadingDashboard.value = false;
  }
}

async function showInternalScope() {
  activeWorkspace.value = 'internal';
  await loadInternalAgents();
}

async function showSettings(section: SettingsSection = 'members') {
  activeWorkspace.value = 'settings';
  activeSettingsSection.value = section;
  if (section === 'members' && users.value.length === 0 && !isLoadingUsers.value) {
    await loadUsers();
  }
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
    activeWorkspace.value = 'internal';
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
    clearPasswordForm();
  } catch (error) {
    passwordChangeError.value = normalizeError(error, 'Không thể đổi mật khẩu lúc này.');
  } finally {
    isChangingPassword.value = false;
  }
}

function clearPasswordForm() {
  currentPassword.value = '';
  newPassword.value = '';
  passwordChangeError.value = '';
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
  activeWorkspace.value = 'internal';
  activeSettingsSection.value = 'members';
  tenants.value = [];
  selectedTenantId.value = '';
  internalAgents.value = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };
  tenantAgents.value = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };
  users.value = [];
  sidebarError.value = '';
  internalAgentsError.value = '';
  tenantAgentsError.value = '';
  userManagementError.value = '';
  closeCreateModal(true);
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

async function handleLogout() {
  authNotice.value = '';
  await logout();
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

function goToPage(page: number) {
  currentPage.value = page;
  if (activeWorkspace.value === 'internal') {
    void loadInternalAgents();
  } else if (activeWorkspace.value === 'tenant' && selectedTenantId.value) {
    void loadTenantAgents(selectedTenantId.value);
  }
}

async function openAgentDetail(agent: AgentSummary, scope: 'internal' | 'tenant') {
  viewMode.value = 'detail';
  agentDetailError.value = '';
  isLoadingAgentDetail.value = true;
  selectedAgent.value = null;

  try {
    if (scope === 'internal') {
      selectedAgent.value = await getInternalAgentDetail(agent.id);
    } else {
      selectedAgent.value = await getTenantAgentDetail(selectedTenantId.value, agent.id);
    }
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }
    agentDetailError.value = normalizeError(error, 'Không tải được chi tiết agent.');
  } finally {
    isLoadingAgentDetail.value = false;
  }
}

function startEditAgent() {
  if (!selectedAgent.value) return;
  editName.value = selectedAgent.value.name;
  editRole.value = selectedAgent.value.role;
  editDescription.value = selectedAgent.value.description ?? '';
  editIcon.value = selectedAgent.value.icon ?? avatarOptions[0]?.id ?? 'mint';
  editStatus.value = selectedAgent.value.status;
  editError.value = '';
  viewMode.value = 'edit';
}

function cancelEdit() {
  viewMode.value = 'detail';
  editError.value = '';
}

async function submitUpdateAgent() {
  if (!selectedAgent.value) return;
  editError.value = '';

  if (!editName.value.trim() || !editRole.value.trim()) {
    editError.value = 'Tên và vai trò là bắt buộc.';
    return;
  }

  const payload: UpdateAgentPayload = {
    name: editName.value.trim(),
    role: editRole.value.trim(),
    description: editDescription.value.trim() || undefined,
    icon: editIcon.value,
    status: editStatus.value
  };

  isSavingAgent.value = true;
  try {
    if (selectedAgent.value.scope === 'Internal') {
      await updateInternalAgent(selectedAgent.value.id, payload);
    } else {
      await updateTenantAgent(selectedTenantId.value, selectedAgent.value.id, payload);
    }
    viewMode.value = 'detail';
    await Promise.all([refreshCurrentAgent(), refreshActiveAgentList()]);
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }
    editError.value = normalizeError(error, 'Không cập nhật được agent.');
  } finally {
    isSavingAgent.value = false;
  }
}

async function refreshActiveAgentList() {
  if (selectedAgent.value?.scope === 'Internal' || activeWorkspace.value === 'internal') {
    await loadInternalAgents();
    return;
  }

  if (selectedTenantId.value) {
    await loadTenantAgents(selectedTenantId.value);
  }
}

async function refreshCurrentAgent() {
  if (!selectedAgent.value) return;
  try {
    if (selectedAgent.value.scope === 'Internal') {
      selectedAgent.value = await getInternalAgentDetail(selectedAgent.value.id);
    } else {
      selectedAgent.value = await getTenantAgentDetail(selectedTenantId.value, selectedAgent.value!.id);
    }
  } catch {
    // silently fail
  }
}

function openDeleteConfirm(agent: AgentSummary) {
  agentToDelete.value = agent;
  isDeleteConfirmModalOpen.value = true;
}

function closeDeleteConfirm() {
  agentToDelete.value = null;
  isDeleteConfirmModalOpen.value = false;
}

async function confirmDeleteAgent() {
  if (!agentToDelete.value) return;
  isDeletingAgent.value = true;

  try {
    if (agentToDelete.value.scope === 'Internal') {
      await deleteInternalAgent(agentToDelete.value.id);
    } else {
      await deleteTenantAgent(selectedTenantId.value, agentToDelete.value.id);
    }
    closeDeleteConfirm();
    viewMode.value = 'list';
    selectedAgent.value = null;
    if (activeWorkspace.value === 'internal') {
      await loadInternalAgents();
    } else if (activeWorkspace.value === 'tenant' && selectedTenantId.value) {
      await loadTenantAgents(selectedTenantId.value);
    }
  } catch (error) {
    if (handleAuthFailure(error)) {
      return;
    }
  } finally {
    isDeletingAgent.value = false;
  }
}

function backToList() {
  viewMode.value = 'list';
  selectedAgent.value = null;
  agentDetailError.value = '';
}
</script>

<template>
  <main class="app-shell">
    <p v-if="isInitializing" class="message">Đang kiểm tra phiên đăng nhập...</p>

    <section v-else-if="!isAuthenticated" class="auth-shell">
      <p v-if="authNotice" class="message auth-shell__notice">{{ authNotice }}</p>
      <LoginForm />
    </section>

    <section v-else class="workspace" :class="{ 'workspace--settings': isSettingsWorkspace }" aria-labelledby="workspace-title">
      <aside class="workspace__sidebar">
        <div class="sidebar__brand">
          <p class="sidebar__eyebrow">Demo AgentWorkSale</p>
          <h1 id="workspace-title">{{ workspaceTitle }}</h1>
          <!-- 
          <p v-if="accessTokenExpiresAt" class="sidebar__meta">Phiên hết hạn: {{ formatDate(accessTokenExpiresAt) }}</p> -->
        </div>

        <nav class="sidebar__nav" aria-label="Khu vực làm việc">
          <button
            class="scope-link"
            :class="{ 'scope-link--active': activeWorkspace === 'internal' }"
            type="button"
            @click="showInternalScope"
          >
            <Shield :size="17" aria-hidden="true" />
            Nội bộ
          </button>
          <button
            class="scope-link"
            :class="{ 'scope-link--active': isSettingsWorkspace }"
            type="button"
            @click="showSettings()"
          >
            <Settings2 :size="17" aria-hidden="true" />
            Thiết lập
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
              :class="{ 'tenant-link--active': activeWorkspace === 'tenant' && selectedTenantId === tenant.id }"
              type="button"
              @click="loadTenantAgents(tenant.id)"
            >
              <Building2 :size="16" aria-hidden="true" />
              <span>{{ tenant.name }}</span>
            </button>

            <p v-if="tenants.length === 0" class="message">Chưa có đơn vị nào để hiển thị.</p>
          </div>
        </section>

        <BaseButton variant="secondary" type="button" @click="handleLogout">
          <LogOut :size="18" aria-hidden="true" />
          Đăng xuất
        </BaseButton>
      </aside>

      <aside v-if="isSettingsWorkspace" class="workspace__settings-sidebar">
        <div class="settings-brand">
          <p class="sidebar__eyebrow">Thiết lập</p>
          <h2>Thiết lập</h2>
          <!-- <p class="sidebar__meta">Quản lý thành viên và đổi mật khẩu từ một menu riêng.</p> -->
        </div>

        <nav class="settings-nav" aria-label="Thiết lập">
          <button
            class="scope-link"
            :class="{ 'scope-link--active': activeSettingsSection === 'members' }"
            type="button"
            @click="showSettings('members')"
          >
            <ShieldCheck :size="17" aria-hidden="true" />
            Quản lý thành viên
          </button>
          <button
            class="scope-link"
            :class="{ 'scope-link--active': activeSettingsSection === 'password' }"
            type="button"
            @click="showSettings('password')"
          >
            <Lock :size="17" aria-hidden="true" />
            Đổi mật khẩu
          </button>
        </nav>
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
          <header v-if="!isSettingsWorkspace" class="content-header">
            <div>
              <p class="content-header__eyebrow">
                {{ workspaceEyebrow }}
              </p>
              <h2>
                {{ workspaceTitle }}
              </h2>
              <p class="content-header__copy">
                {{
                  activeWorkspace === 'internal'
                    ? 'Chỉ quản trị viên được xem và tạo các agent phục vụ nội bộ.'
                    : 'Mỗi đơn vị có một danh sách agent riêng, tách biệt với agent nội bộ.'
                }}
              </p>
            </div>

            <BaseButton
              v-if="activeWorkspace === 'internal'"
              type="button"
              :disabled="Boolean(internalAgentsError)"
              @click="openCreateModal"
            >
              <Plus :size="18" aria-hidden="true" />
              Thêm agent
            </BaseButton>
          </header>

          <template v-if="isSettingsWorkspace">
            <header class="content-header">
              <div>
                <p class="content-header__eyebrow">Thiết lập</p>
                <h2>{{ settingsSectionTitle }}</h2>
                <p class="content-header__copy">
                  {{
                    activeSettingsSection === 'members'
                      ? 'Lock/Unlock người dùng'
                      : 'Đổi mật khẩu sẽ thu hồi phiên cũ'
                  }}
                </p>
              </div>

              <BaseButton
                v-if="activeSettingsSection === 'members'"
                variant="secondary"
                type="button"
                :disabled="isLoadingUsers"
                @click="loadUsers()"
              >
                <RefreshCw :size="18" :class="{ spin: isLoadingUsers }" aria-hidden="true" />
                Tải lại
              </BaseButton>
            </header>

            <div v-if="activeSettingsSection === 'members'" class="content-panel user-panel">
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
            </div>

            <div v-else class="content-panel settings-form-panel">
              <!-- <p class="message settings-form-panel__lead">
                Hãy nhập mật khẩu hiện tại và mật khẩu mới để cập nhật tài khoản của bạn.
              </p> -->
              <form class="create-agent" @submit.prevent="submitPasswordChange">
                <div class="create-agent__group">
                  <label class="create-agent__label" for="current-password">Mật khẩu hiện tại</label>
                  <BaseInput
                    id="current-password"
                    v-model="currentPassword"
                    type="password"
                    autocomplete="current-password"
                    placeholder="Nhập mật khẩu hiện tại"
                  />
                </div>

                <div class="create-agent__group">
                  <label class="create-agent__label" for="new-password">Mật khẩu mới</label>
                  <BaseInput
                    id="new-password"
                    v-model="newPassword"
                    type="password"
                    autocomplete="new-password"
                    placeholder="Nhập mật khẩu mới"
                  />
                </div>

                <p v-if="passwordChangeError" class="message message--error">{{ passwordChangeError }}</p>

                <div class="create-agent__actions">
                  <BaseButton variant="secondary" type="button" :disabled="isChangingPassword" @click="clearPasswordForm">
                    Xóa
                  </BaseButton>
                  <BaseButton type="submit" :disabled="isChangingPassword">
                    {{ isChangingPassword ? 'Đang cập nhật...' : 'Xác nhận đổi mật khẩu' }}
                  </BaseButton>
                </div>
              </form>
            </div>
          </template>

          <template v-else>
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

            <div v-if="activeWorkspace === 'internal'" class="content-panel">
              <p v-if="internalAgentsError" class="message message--error">{{ internalAgentsError }}</p>
              <div v-else-if="isLoadingDashboard" class="loading-row">
                <LoaderCircle :size="18" class="spin" aria-hidden="true" />
                <span>Đang tải agent nội bộ...</span>
              </div>
              <div v-else-if="internalAgents.items.length === 0" class="empty-card">
                <h3>{{ emptyStateTitle }}</h3>
                <p>{{ emptyStateDescription }}</p>
              </div>
              <div v-else class="agent-grid">
                <article v-for="agent in internalAgents.items" :key="agent.id" class="agent-card" @click="openAgentDetail(agent, 'internal')">
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
              <div v-if="internalAgents.totalPages > 1" class="pagination">
                <BaseButton
                  variant="secondary"
                  type="button"
                  :disabled="currentPage <= 1"
                  @click="goToPage(currentPage - 1)"
                >
                  Trước
                </BaseButton>
                <span class="pagination-info">Trang {{ internalAgents.page }} / {{ internalAgents.totalPages }}</span>
                <BaseButton
                  variant="secondary"
                  type="button"
                  :disabled="currentPage >= internalAgents.totalPages"
                  @click="goToPage(currentPage + 1)"
                >
                  Sau
                </BaseButton>
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
              <div v-else-if="tenantAgents.items.length === 0" class="empty-card">
                <h3>{{ emptyStateTitle }}</h3>
                <p>{{ emptyStateDescription }}</p>
              </div>
              <div v-else class="agent-grid">
                <article v-for="agent in tenantAgents.items" :key="agent.id" class="agent-card" @click="openAgentDetail(agent, 'tenant')">
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
              <div v-if="tenantAgents.totalPages > 1" class="pagination">
                <BaseButton
                  variant="secondary"
                  type="button"
                  :disabled="currentPage <= 1"
                  @click="goToPage(currentPage - 1)"
                >
                  Trước
                </BaseButton>
                <span class="pagination-info">Trang {{ tenantAgents.page }} / {{ tenantAgents.totalPages }}</span>
                <BaseButton
                  variant="secondary"
                  type="button"
                  :disabled="currentPage >= tenantAgents.totalPages"
                  @click="goToPage(currentPage + 1)"
                >
                  Sau
                </BaseButton>
              </div>
            </div>
          </template>
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

    <BaseModal :open="viewMode !== 'list' && activeWorkspace !== 'settings'" title="" @close="backToList">
      <div v-if="isLoadingAgentDetail" class="loading-row">
        <LoaderCircle :size="18" class="spin" aria-hidden="true" />
        <span>Đang tải chi tiết agent...</span>
      </div>
      <div v-else-if="agentDetailError" class="message message--error">{{ agentDetailError }}</div>
      <template v-else-if="selectedAgent">
        <template v-if="viewMode === 'detail'">
          <div class="agent-detail">
            <div class="agent-detail__header">
              <div class="agent-card__avatar" :style="avatarStyle(selectedAgent.icon)">{{ avatarLabel(selectedAgent) }}</div>
              <div>
                <h3>{{ selectedAgent.name }}</h3>
                <p>{{ selectedAgent.description || 'Chưa có mô tả.' }}</p>
              </div>
            </div>
            <dl class="agent-detail__fields">
              <div>
                <dt>Mã agent</dt>
                <dd>{{ selectedAgent.code }}</dd>
              </div>
              <div>
                <dt>Vai trò</dt>
                <dd>{{ selectedAgent.role }}</dd>
              </div>
              <div>
                <dt>Phạm vi</dt>
                <dd>{{ selectedAgent.scope }}</dd>
              </div>
              <div>
                <dt>Trạng thái</dt>
                <dd><span :class="statusTone(selectedAgent.status)">{{ selectedAgent.status }}</span></dd>
              </div>
              <div>
                <dt>Ngày tạo</dt>
                <dd>{{ formatDate(selectedAgent.createdAt) }}</dd>
              </div>
              <div v-if="selectedAgent.modifiedAt">
                <dt>Sửa lần cuối</dt>
                <dd>{{ formatDate(selectedAgent.modifiedAt) }}</dd>
              </div>
            </dl>
            <div class="agent-detail__actions">
              <BaseButton variant="secondary" type="button" @click="backToList">Quay lại</BaseButton>
              <BaseButton variant="secondary" type="button" @click="startEditAgent">Chỉnh sửa</BaseButton>
              <BaseButton variant="danger" type="button" @click="openDeleteConfirm(selectedAgent)">Xóa</BaseButton>
            </div>
          </div>
        </template>
        <template v-else-if="viewMode === 'edit'">
          <div class="create-agent">
            <div class="create-agent__group">
              <p class="create-agent__label">Hình đại diện</p>
              <div class="avatar-picker">
                <button
                  v-for="option in avatarOptions"
                  :key="option.id"
                  class="avatar-choice"
                  :class="{ 'avatar-choice--active': editIcon === option.id }"
                  :style="{ background: option.accent }"
                  type="button"
                  @click="editIcon = option.id"
                >
                  {{ option.label }}
                </button>
              </div>
            </div>

            <div class="create-agent__group">
              <label class="create-agent__label" for="edit-name">Tên</label>
              <BaseInput id="edit-name" v-model="editName" placeholder="Nhập tên" />
            </div>

            <div class="create-agent__group">
              <label class="create-agent__label" for="edit-role">Vai trò</label>
              <textarea
                id="edit-role"
                v-model="editRole"
                class="agent-textarea"
                rows="3"
                placeholder="Nhập mô tả vai trò"
              />
            </div>

            <div class="create-agent__group">
              <label class="create-agent__label" for="edit-description">Mô tả</label>
              <textarea
                id="edit-description"
                v-model="editDescription"
                class="agent-textarea"
                rows="4"
                placeholder="Mô tả ngắn về nhiệm vụ hoặc chuyên môn của agent"
              />
            </div>

            <div class="create-agent__group">
              <label class="create-agent__label" for="edit-status">Trạng thái</label>
              <select id="edit-status" v-model="editStatus" class="agent-select">
                <option v-for="option in allStatusOptions" :key="option.value" :value="option.value">
                  {{ option.label }}
                </option>
              </select>
            </div>

            <p v-if="editError" class="message message--error">{{ editError }}</p>

            <div class="create-agent__actions">
              <BaseButton variant="secondary" type="button" :disabled="isSavingAgent" @click="cancelEdit">Hủy</BaseButton>
              <BaseButton type="button" :disabled="isSavingAgent" @click="submitUpdateAgent">
                {{ isSavingAgent ? 'Đang lưu...' : 'Lưu' }}
              </BaseButton>
            </div>
          </div>
        </template>
      </template>
    </BaseModal>

    <BaseModal :open="isDeleteConfirmModalOpen" title="Xác nhận xóa" @close="closeDeleteConfirm">
      <div class="delete-confirm">
        <p>Bạn có chắc chắn muốn xóa agent <strong>{{ agentToDelete?.name }}</strong>?</p>
        <p>Hành động này không thể hoàn tác.</p>
        <div class="create-agent__actions">
          <BaseButton variant="secondary" type="button" :disabled="isDeletingAgent" @click="closeDeleteConfirm">Hủy</BaseButton>
          <BaseButton variant="danger" type="button" :disabled="isDeletingAgent" @click="confirmDeleteAgent">
            {{ isDeletingAgent ? 'Đang xóa...' : 'Xác nhận xóa' }}
          </BaseButton>
        </div>
      </div>
    </BaseModal>
  </main>
</template>

<style scoped>
.agent-card {
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.15s ease;
}

.agent-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
  margin-top: 1.5rem;
  padding-top: 1rem;
  border-top: 1px solid #e9ecef;
}

.pagination-info {
  font-size: 0.875rem;
  color: #6c757d;
}

.agent-detail {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.agent-detail__header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.agent-detail__fields {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}

.agent-detail__fields div {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.agent-detail__fields dt {
  font-size: 0.75rem;
  color: #6c757d;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.agent-detail__fields dd {
  margin: 0;
  font-weight: 500;
}

.agent-detail__actions {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #e9ecef;
}

.agent-select {
  width: 100%;
  padding: 0.5rem 0.75rem;
  border: 1px solid #dee2e6;
  border-radius: 0.375rem;
  background-color: #fff;
  font-size: 0.875rem;
}

.delete-confirm {
  text-align: center;
}

.delete-confirm p {
  margin: 0.5rem 0;
}

.delete-confirm strong {
  color: #dc3545;
}
</style>
