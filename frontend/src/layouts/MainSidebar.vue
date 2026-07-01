<script setup lang="ts">
import { Building2, LogOut, Settings2, Shield } from '@lucide/vue';
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
}>();

const emit = defineEmits<{
  selectTenant: [tenantId: string];
  logout: [];
}>();
</script>

<template>
  <aside class="workspace__sidebar">
    <nav class="sidebar__nav" aria-label="Khu vực làm việc">
      <RouterLink
        class="scope-link"
        :class="{ 'scope-link--active': activeRouteName === 'agents-internal' }"
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
          :class="{ 'tenant-link--active': routeTenantId === tenant.id }"
          :to="{ name: 'agents-tenant', params: { tenantId: tenant.id } }"
          @click="emit('selectTenant', tenant.id)"
        >
          <Building2 :size="16" aria-hidden="true" />
          <span>{{ tenant.name }}</span>
        </RouterLink>
        <p v-if="tenants.length === 0" class="message">Chưa có đơn vị nào.</p>
      </div>
    </section>

    <BaseButton variant="secondary" type="button" @click="emit('logout')">
      <LogOut :size="18" aria-hidden="true" />
      Đăng xuất
    </BaseButton>
  </aside>
</template>
