<script setup lang="ts">
import { RouterLink } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
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

defineProps<{
  activeRouteName: string | symbol | null | undefined;
  routeTenantId: string | string[] | undefined;
  isSettingsRoute: boolean;
  tenants: TenantSummary[];
  sidebarError: string;
  isLoadingTenants: boolean;
  isCollapsed: boolean;
}>();

const emit = defineEmits<{
  selectTenant: [tenantId: string];
  logout: [];
  toggleSidebar: [];
}>();
</script>

<template>
  <aside class="workspace__sidebar" :class="{ 'workspace__sidebar--collapsed': isCollapsed }">
    <div class="workspace__sidebar-content">
      <nav class="sidebar__nav" aria-label="Khu vực làm việc">
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': activeRouteName === 'dashboard' }"
          :to="{ name: 'dashboard' }"
        >
          <IconHome :size="18" stroke-width="1.5" aria-hidden="true" />
          <span>Tổng quan</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': activeRouteName === 'agents-internal' }"
          :to="{ name: 'agents-internal' }"
        >
          <IconUserStar :size="18" stroke-width="1.5" aria-hidden="true" />
          <span>Nhân viên AI</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': isSettingsRoute }"
          :to="{ name: 'settings' }"
        >
          <IconSettings :size="18" stroke-width="1.5" aria-hidden="true" />
          <span>Thiết lập</span>
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
            :class="{ 'tenant-link--active': routeTenantId === tenant.id }"
            :to="{ name: 'agents-tenant', params: { tenantId: tenant.id } }"
            @click="emit('selectTenant', tenant.id)"
          >
            <IconBuildingStore :size="18" stroke-width="1.5" aria-hidden="true" />
            <span>{{ tenant.name }}</span>
          </RouterLink>
          <p v-if="tenants.length === 0" class="message">Chưa có đơn vị nào.</p>
        </div>
      </section>
    </div>

    <div class="workspace__sidebar-action">
      <BaseButton class="workspace__sidebar-action-button" variant="secondary" type="button" @click="emit('logout')">
        <IconLogout :size="18" stroke-width="1.5" aria-hidden="true" />
        <span class="workspace__sidebar-action-label">Đăng xuất</span>
      </BaseButton>
    </div>

    <div class="workspace__sidebar-footer">
      <BaseButton
        class="workspace__sidebar-footer-button workspace__sidebar-footer-button--icon"
        variant="secondary"
        type="button"
        :aria-label="isCollapsed ? 'Mở rộng sidebar' : 'Thu gọn sidebar'"
        :title="isCollapsed ? 'Mở rộng sidebar' : 'Thu gọn sidebar'"
        @click="emit('toggleSidebar')"
      >
        <IconLayoutSidebarLeftExpand v-if="isCollapsed" :size="18" stroke-width="1.5" aria-hidden="true" />
        <IconLayoutSidebarLeftCollapse v-else :size="18" stroke-width="1.5" aria-hidden="true" />
      </BaseButton>
    </div>
  </aside>
</template>
