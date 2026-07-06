<script setup lang="ts">
import { ChevronLeft, ChevronRight, LogOut } from '../icons/tabler';
import { RouterLink } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import type { TenantSummary } from '../api';

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
          <i class="ti ti-home" aria-hidden="true"></i>
          <span>Tổng quan</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': activeRouteName === 'agents-internal' }"
          :to="{ name: 'agents-internal' }"
        >
          <i class="ti ti-user-star" aria-hidden="true"></i>
          <span>Nhân viên AI</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': isSettingsRoute }"
          :to="{ name: 'settings' }"
        >
          <i class="ti ti-settings" aria-hidden="true"></i>
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
            <i class="ti ti-building-store" aria-hidden="true"></i>
            <span>{{ tenant.name }}</span>
          </RouterLink>
          <p v-if="tenants.length === 0" class="message">Chưa có đơn vị nào.</p>
        </div>
      </section>
    </div>

    <div class="workspace__sidebar-action">
      <BaseButton class="workspace__sidebar-action-button" variant="secondary" type="button" @click="emit('logout')">
        <LogOut :size="18" aria-hidden="true" />
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
        <i v-if="isCollapsed" class="ti ti-layout-sidebar-left-expand" aria-hidden="true"></i>
        <i v-else class="ti ti-layout-sidebar-left-collapse" aria-hidden="true"></i>
      </BaseButton>
    </div>
  </aside>
</template>
