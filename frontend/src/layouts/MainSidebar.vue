<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';
import type { TenantSummary } from '../api';
import {
  IconBuildingStore,
  IconHome,
  IconLayoutSidebarLeftCollapse,
  IconLayoutSidebarLeftExpand,
  IconLogout,
  IconSettings,
  IconUserStar
} from '@tabler/icons-vue';

const emit = defineEmits<{
  selectTenant: [tenantId: string];
  logout: [];
  toggleSidebar: [];
}>();

const props = defineProps<{
  activeRouteName: string | symbol | null | undefined;
  routeTenantId: string | string[] | undefined;
  isSettingsRoute: boolean;
  tenants: TenantSummary[];
  sidebarError: string;
  isLoadingTenants: boolean;
  isCollapsed: boolean;
}>();

const mainItems = computed(() => [
  {
    key: 'dashboard',
    label: 'Tổng quan',
    to: { name: 'dashboard' as const },
    isActive: props.activeRouteName === 'dashboard',
    icon: IconHome
  },
  {
    key: 'agents-internal',
    label: 'Nhân viên AI',
    to: { name: 'agents-internal' as const },
    isActive: props.activeRouteName === 'agents-internal',
    icon: IconUserStar
  },
  {
    key: 'settings',
    label: 'Thiết lập',
    to: { name: 'settings' as const },
    isActive: props.isSettingsRoute,
    icon: IconSettings
  }
]);
</script>

<template>
  <aside class="workspace__sidebar" :class="{ 'workspace__sidebar--collapsed': props.isCollapsed }">
    <div class="workspace__sidebar-content">
      <nav class="sidebar__nav" aria-label="Khu vực làm việc">
        <div
          v-for="item in mainItems"
          :key="item.key"
          class="sidebar__item"
          :class="{ 'sidebar__item--collapsed': props.isCollapsed }"
        >
          <RouterLink
            class="scope-link"
            :class="{ 'scope-link--active': item.isActive }"
            :to="item.to"
          >
            <component
              :is="item.icon"
              v-if="item.icon"
              :size="16"
              stroke-width="1.5"
              aria-hidden="true"
            />
            <i v-else class="ti ti-test-pipe" aria-hidden="true"></i>
            <span>{{ item.label }}</span>
          </RouterLink>
        </div>
      </nav>

      <section class="tenant-list" aria-labelledby="tenant-list-title">
        <div class="tenant-list__header">
          <h2 id="tenant-list-title">Đơn vị</h2>
        </div>
        <p v-if="props.sidebarError" class="message message--error">{{ props.sidebarError }}</p>
        <p v-else-if="props.isLoadingTenants && props.tenants.length === 0" class="message">Đang tải danh sách đơn vị...</p>
        <div v-else class="tenant-list__items">
          <RouterLink
            v-for="tenant in props.tenants"
            :key="tenant.id"
            class="tenant-link"
            :class="{ 'tenant-link--active': props.routeTenantId === tenant.id }"
            :to="{ name: 'agents-tenant', params: { tenantId: tenant.id } }"
            @click="emit('selectTenant', tenant.id)"
          >
            <IconBuildingStore :size="16" stroke-width="1.5" aria-hidden="true" />
            <span>{{ tenant.name }}</span>
          </RouterLink>
          <p v-if="props.tenants.length === 0" class="message">Chưa có đơn vị nào.</p>
        </div>
      </section>
    </div>

    <div class="workspace__sidebar-action">
      <BaseButton class="workspace__sidebar-action-button" variant="secondary" type="button" @click="emit('logout')">
        <IconLogout :size="16" stroke-width="1.5" aria-hidden="true" />
        <span class="workspace__sidebar-action-label">Đăng xuất</span>
      </BaseButton>
    </div>

    <div class="workspace__sidebar-footer">
      <BaseButton
        class="workspace__sidebar-footer-button workspace__sidebar-footer-button--icon"
        variant="secondary"
        type="button"
        :aria-label="props.isCollapsed ? 'Mở rộng sidebar' : 'Thu gọn sidebar'"
        :title="props.isCollapsed ? 'Mở rộng sidebar' : 'Thu gọn sidebar'"
        @click="emit('toggleSidebar')"
      >
        <IconLayoutSidebarLeftExpand v-if="props.isCollapsed" :size="16" stroke-width="1.5" aria-hidden="true" />
        <IconLayoutSidebarLeftCollapse v-else :size="16" stroke-width="1.5" aria-hidden="true" />
      </BaseButton>
    </div>
  </aside>
</template>
