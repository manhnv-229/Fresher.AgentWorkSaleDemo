<script setup lang="ts">
import { Building2, LoaderCircle, LogOut, MoreHorizontal, RefreshCw, Settings2, Shield } from '@lucide/vue';
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import { useAuth } from '../composables/useAuth';
import { useAgentDetail } from '../composables/useAgentDetail';
import { useAgentDetailEditor } from '../composables/useAgentDetailEditor';
import { useTenantSelection } from '../composables/useTenantSelection';
import { ApiError } from '../api/http';

const { isAuthenticated, isInitializing, initializeAuth, logout, clearSession } = useAuth();
const { tenants, selectedTenantId, loadTenants, selectTenant } = useTenantSelection();
const route = useRoute();
const router = useRouter();
const { beginEdit, cancelEdit, saveDraft, saveAndPublish, toggleActivation, isEditing, isSaving } = useAgentDetailEditor();

const sidebarError = ref('');
const isLoadingTenants = ref(false);
const isAgentMenuOpen = ref(false);

const isSettingsRoute = computed(() => route.path.startsWith('/settings'));
const isAgentRoute = computed(() => route.name === 'agent-detail' || route.name === 'agent-knowledge');
const isAgentDetailRoute = computed(() => route.name === 'agent-detail');
const canToggleAgentStatus = computed(() => {
  const status = agent.value?.status;
  return status === 'Active' || status === 'Published' || status === 'Inactive' || status === 'Draft';
});
const agentStatusActionLabel = computed(() => {
  const status = agent.value?.status;
  return status === 'Active' || status === 'Published' ? 'Ngừng hoạt động' : 'Kích hoạt';
});

const { agent, isLoading: isLoadingAgent, loadInternal, loadTenant, clear: clearAgent } = useAgentDetail();

const agentScope = computed(() => (route.query.scope as string) || 'internal');
const agentTenantId = computed(() => (route.query.tenantId as string) || '');

function buildAgentSectionLink(section: string) {
  const name = section === 'knowledge' ? 'agent-knowledge' : 'agent-detail';
  return {
    name,
    params: { agentId: route.params.agentId },
    query: { ...route.query }
  };
}

onMounted(() => {
  document.addEventListener('pointerdown', handlePointerDown);
  void initializeAuth();
});

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', handlePointerDown);
});

watch(isAuthenticated, async (authenticated) => {
  if (authenticated) {
    sidebarError.value = '';
    isLoadingTenants.value = true;
    try {
      await loadTenants();
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        clearSession();
        return;
      }
      sidebarError.value = 'Không tải được danh sách đơn vị.';
    } finally {
      isLoadingTenants.value = false;
    }
  }
}, { immediate: true });

watch(isAgentRoute, async (isAgent) => {
  isAgentMenuOpen.value = false;
  if (isAgent) {
    clearAgent();
    const id = route.params.agentId as string;
    if (agentScope.value === 'tenant' && agentTenantId.value) {
      await loadTenant(agentTenantId.value, id);
    } else {
      await loadInternal(id);
    }
  } else {
    clearAgent();
  }
}, { immediate: true });

watch(() => route.fullPath, () => {
  isAgentMenuOpen.value = false;
});

function handleTenantClick(tenantId: string) {
  selectTenant(tenantId);
  router.push({ name: 'agents-tenant', params: { tenantId } });
}

async function handleLogout() {
  await logout();
  router.push({ name: 'login' });
}

function goBackFromAgent() {
  const scope = (route.query.scope as string) || 'internal';
  const tenantId = (route.query.tenantId as string) || '';
  if (scope === 'tenant' && tenantId) {
    router.push({ name: 'agents-tenant', params: { tenantId } });
  } else {
    router.push({ name: 'agents-internal' });
  }
}

function toggleAgentMenu() {
  if (!isAgentDetailRoute.value || !canToggleAgentStatus.value || isLoadingAgent.value) return;
  isAgentMenuOpen.value = !isAgentMenuOpen.value;
}

async function handleAgentStatusAction() {
  isAgentMenuOpen.value = false;
  await toggleActivation();
}

function handlePointerDown(event: PointerEvent) {
  const target = event.target;
  if (!(target instanceof HTMLElement)) return;
  if (target.closest('.agent-header__menu')) return;
  isAgentMenuOpen.value = false;
}
</script>

<template>
  <main v-if="isInitializing" class="app-shell">
    <p class="message">Đang kiểm tra phiên đăng nhập...</p>
  </main>

  <main v-else-if="!isAuthenticated" class="app-shell">
    <p class="message">Đang chuyển tới trang đăng nhập...</p>
  </main>

  <section v-else class="workspace" :class="{ 'workspace--settings': isSettingsRoute, 'workspace--agent': isAgentRoute }" aria-labelledby="workspace-title">
    <header v-if="!isAgentRoute" class="workspace-header">
      <span id="workspace-title" class="workspace-header__title">Demo AgentWorkSale</span>
    </header>
    <header v-if="isAgentRoute" class="agent-header">
      <div class="agent-header__info">
        <div class="agent-header__title">
          <span v-if="isLoadingAgent">Đang tải...</span>
          <span v-else-if="agent">{{ agent.name }}</span>
          <span v-else>Agent</span>
        </div>
        <div class="agent-header__role">
          <span v-if="isLoadingAgent">...</span>
          <span v-else-if="agent">{{ agent.role }}</span>
          <span v-else>Không tải được thông tin agent.</span>
        </div>
      </div>
      <div class="agent-header__actions">
        <BaseButton
          v-if="isAgentDetailRoute && !isEditing"
          class="agent-header__edit-button"
          variant="secondary"
          type="button"
          :disabled="isLoadingAgent"
          @click="beginEdit"
        >
          Sửa
        </BaseButton>
        <div v-if="isAgentDetailRoute" class="agent-header__menu">
          <button
            type="button"
            class="agent-header__menu-trigger"
            :disabled="!canToggleAgentStatus || isSaving || isLoadingAgent"
            :aria-expanded="isAgentMenuOpen"
            aria-haspopup="menu"
            @click="toggleAgentMenu"
          >
            <MoreHorizontal :size="16" aria-hidden="true" />
          </button>
          <div v-if="isAgentMenuOpen && canToggleAgentStatus" class="agent-header__menu-panel" role="menu">
            <button
              type="button"
              class="agent-header__menu-item"
              role="menuitem"
              :disabled="isSaving"
              @click="handleAgentStatusAction"
            >
              {{ agentStatusActionLabel }}
            </button>
          </div>
        </div>
        <button type="button" class="agent-header__close" @click="goBackFromAgent">&times;</button>
      </div>
    </header>

    <aside v-if="!isAgentRoute" class="workspace__sidebar">
      <nav class="sidebar__nav" aria-label="Khu vực làm việc">
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': route.name === 'agents-internal' }"
          :to="{ name: 'agents-internal' }"
        >
          <Shield :size="17" aria-hidden="true" />
          Nhân viên AI
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': isSettingsRoute }"
          :to="{ name: 'settings-members' }"
        >
          <Settings2 :size="17" aria-hidden="true" />
          Thiết lập
        </RouterLink>
      </nav>

      <section class="tenant-list" aria-labelledby="tenant-list-title">
        <div class="tenant-list__header">
          <h2 id="tenant-list-title">Đơn vị</h2>
        </div>

        <p v-if="sidebarError" class="message message--error">{{ sidebarError }}</p>
        <p v-else-if="isLoadingTenants && tenants.length === 0" class="message">Đang tải danh sách đơn vị...</p>
        <div v-else class="tenant-list__items">
          <RouterLink
            v-for="tenant in tenants"
            :key="tenant.id"
            class="tenant-link"
            :class="{ 'tenant-link--active': route.params.tenantId === tenant.id }"
            :to="{ name: 'agents-tenant', params: { tenantId: tenant.id } }"
            @click="selectTenant(tenant.id)"
          >
            <Building2 :size="16" aria-hidden="true" />
            <span>{{ tenant.name }}</span>
          </RouterLink>
          <p v-if="tenants.length === 0" class="message">Chưa có đơn vị nào.</p>
        </div>
      </section>

      <BaseButton variant="secondary" type="button" @click="handleLogout">
        <LogOut :size="18" aria-hidden="true" />
        Đăng xuất
      </BaseButton>
    </aside>

    <aside v-if="isSettingsRoute" class="workspace__settings-sidebar">
      <nav class="settings-nav" aria-label="Thiết lập">
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': route.name === 'settings-members' }"
          :to="{ name: 'settings-members' }"
        >
          Quản lý thành viên
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': route.name === 'settings-password' }"
          :to="{ name: 'settings-password' }"
        >
          Đổi mật khẩu
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': route.name === 'settings-audit-log' }"
          :to="{ name: 'settings-audit-log' }"
        >
          Nhật ký hoạt động
        </RouterLink>
      </nav>
    </aside>

    <aside v-if="isAgentRoute" class="workspace__agent-sidebar">
      <nav class="agent-sidebar__nav" aria-label="Agent workspace">
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': route.name === 'agent-detail' }"
          :to="buildAgentSectionLink('info')"
        >
          Thông tin chung
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': route.name === 'agent-knowledge' }"
          :to="buildAgentSectionLink('knowledge')"
        >
          Tri thức
        </RouterLink>
      </nav>
    </aside>

    <section class="workspace__content">
      <div class="workspace__page">
        <RouterView />
      </div>
    </section>

    <footer v-if="isAgentDetailRoute" class="agent-footer">
      <div class="agent-footer__actions">
        <template v-if="isEditing">
        <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="cancelEdit">Hủy</BaseButton>
        <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="saveDraft">Lưu nháp</BaseButton>
        <BaseButton type="button" :disabled="isSaving" @click="saveAndPublish">
          {{ isSaving ? 'Đang lưu...' : 'Lưu và Phát hành' }}
        </BaseButton>
        </template>
      </div>
    </footer>
  </section>
</template>
