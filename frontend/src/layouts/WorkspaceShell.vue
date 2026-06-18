<script setup lang="ts">
import { Building2, LoaderCircle, LogOut, RefreshCw, Settings2, Shield } from '@lucide/vue';
import { computed, onMounted, ref, watch } from 'vue';
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import { useAuth } from '../composables/useAuth';
import { useTenantSelection } from '../composables/useTenantSelection';
import { ApiError } from '../api/http';

const { isAuthenticated, isInitializing, initializeAuth, logout, clearSession } = useAuth();
const { tenants, selectedTenantId, loadTenants, selectTenant } = useTenantSelection();
const route = useRoute();
const router = useRouter();

const sidebarError = ref('');
const isLoadingTenants = ref(false);

const isSettingsRoute = computed(() => route.path.startsWith('/settings'));
const workspaceTitle = computed(() => {
  if (isSettingsRoute.value) return 'Thiết lập';
  if (route.name === 'agents-internal') return 'Agent nội bộ';
  if (route.name === 'agents-tenant') {
    const tenant = tenants.value.find(t => t.id === route.params.tenantId);
    return tenant?.name || 'Đơn vị';
  }
  if (route.name === 'agent-detail') return 'Chỉnh sửa agent';
  return 'Agent nội bộ';
});

onMounted(() => {
  void initializeAuth();
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

function handleTenantClick(tenantId: string) {
  selectTenant(tenantId);
  router.push({ name: 'agents-tenant', params: { tenantId } });
}

async function handleLogout() {
  await logout();
  router.push({ name: 'login' });
}
</script>

<template>
  <main v-if="isInitializing" class="app-shell">
    <p class="message">Đang kiểm tra phiên đăng nhập...</p>
  </main>

  <main v-else-if="!isAuthenticated" class="app-shell">
    <p class="message">Đang chuyển tới trang đăng nhập...</p>
  </main>

  <section v-else class="workspace" :class="{ 'workspace--settings': isSettingsRoute }" aria-labelledby="workspace-title">
    <aside class="workspace__sidebar">
      <div class="sidebar__brand">
        <p class="sidebar__eyebrow">Demo AgentWorkSale</p>
        <h1 id="workspace-title">{{ workspaceTitle }}</h1>
      </div>

      <nav class="sidebar__nav" aria-label="Khu vực làm việc">
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': route.name === 'agents-internal' }"
          :to="{ name: 'agents-internal' }"
        >
          <Shield :size="17" aria-hidden="true" />
          Nội bộ
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
      <div class="settings-brand">
        <p class="sidebar__eyebrow">Thiết lập</p>
        <h2>Thiết lập</h2>
      </div>

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
      </nav>
    </aside>

    <section class="workspace__content">
      <RouterView />
    </section>
  </section>
</template>
