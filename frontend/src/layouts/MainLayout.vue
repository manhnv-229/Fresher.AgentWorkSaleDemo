<script setup lang="ts">
import { LoaderCircle, RefreshCw } from '@lucide/vue';
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { RouterView, useRoute, useRouter } from 'vue-router';
import AgentFooter from './AgentFooter.vue';
import AgentSidebar from './AgentSidebar.vue';
import AppHeader from './AppHeader.vue';
import MainSidebar from './MainSidebar.vue';
import SettingsSidebar from './SettingsSidebar.vue';
import { useAuth } from '../composables/useAuth';
import { useAgentDetail } from '../composables/useAgentDetail';
import { useAgentDetailEditor } from '../composables/useAgentDetailEditor';
import { useTenantSelection } from '../composables/useTenantSelection';
import { ApiError } from '../api/http';

const { isAuthenticated, isInitializing, initializeAuth, logout, clearSession } = useAuth();
const { tenants, loadTenants, selectTenant } = useTenantSelection();
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
    <AppHeader
      :is-agent-route="isAgentRoute"
      :is-agent-detail-route="isAgentDetailRoute"
      :is-loading-agent="isLoadingAgent"
      :agent="agent"
      :is-editing="isEditing"
      :is-saving="isSaving"
      :is-agent-menu-open="isAgentMenuOpen"
      :can-toggle-agent-status="canToggleAgentStatus"
      :agent-status-action-label="agentStatusActionLabel"
      @begin-edit="beginEdit"
      @toggle-agent-menu="toggleAgentMenu"
      @agent-status-action="handleAgentStatusAction"
      @close-agent="goBackFromAgent"
    />

    <MainSidebar
      v-if="!isAgentRoute"
      :active-route-name="route.name"
      :route-tenant-id="route.params.tenantId"
      :is-settings-route="isSettingsRoute"
      :tenants="tenants"
      :sidebar-error="sidebarError"
      :is-loading-tenants="isLoadingTenants"
      @select-tenant="selectTenant"
      @logout="handleLogout"
    />

    <SettingsSidebar v-if="isSettingsRoute" :active-route-name="route.name" />

    <AgentSidebar
      v-if="isAgentRoute"
      :active-route-name="route.name"
      :detail-link="buildAgentSectionLink('info')"
      :knowledge-link="buildAgentSectionLink('knowledge')"
    />

    <section class="workspace__content">
      <div class="workspace__page">
        <RouterView />
      </div>
    </section>

    <AgentFooter
      v-if="isAgentDetailRoute"
      :is-editing="isEditing"
      :is-saving="isSaving"
      @cancel-edit="cancelEdit"
      @save-draft="saveDraft"
      @save-and-publish="saveAndPublish"
    />
  </section>
</template>
