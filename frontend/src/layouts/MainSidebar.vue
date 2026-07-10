<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';
import type { TenantSummary } from '../api';
import { useI18n } from '../i18n';
import {
  IconBuildingStore,
  IconHome,
  IconLayoutSidebarLeftCollapse,
  IconLayoutSidebarLeftExpand,
  IconLogout,
  IconSettings,
  IconUserStar,
  IconSmartHome
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

const { t } = useI18n();

const mainItems = computed(() => [
  {
    key: 'dashboard',
    label: t('nav.dashboard'),
    to: { name: 'dashboard' as const },
    isActive: props.activeRouteName === 'dashboard',
    icon: IconSmartHome
  },
  {
    key: 'agents',
    label: t('nav.aiAgents'),
    to: { name: 'agents' as const },
    isActive: props.activeRouteName === 'agents' || props.activeRouteName === 'agents-external',
    icon: IconUserStar
  },
  {
    key: 'settings',
    label: t('nav.settings'),
    to: { name: 'settings-members' as const },
    isActive: props.isSettingsRoute,
    icon: IconSettings
  }
]);
</script>

<template>
  <aside class="workspace__sidebar" :class="{ 'workspace__sidebar--collapsed': props.isCollapsed }">
    <div class="workspace__sidebar-content">
      <nav class="sidebar__nav" :aria-label="t('nav.workspace')">
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
              :size="20"
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
          <h2 id="tenant-list-title">{{ t('nav.unit') }}</h2>
        </div>
        <p v-if="props.sidebarError" class="message message--error">{{ props.sidebarError }}</p>
        <p v-else-if="props.isLoadingTenants && props.tenants.length === 0" class="message">{{ t('states.loadingTenantList') }}</p>
        <div v-else class="tenant-list__items">
          <RouterLink
            v-for="tenant in props.tenants"
            :key="tenant.id"
            class="tenant-link"
            :class="{ 'tenant-link--active': props.routeTenantId === tenant.id }"
            :to="{ name: 'agents-tenant', params: { tenantId: tenant.id } }"
            @click="emit('selectTenant', tenant.id)"
          >
            <IconBuildingStore :size="24" stroke-width="1.5" aria-hidden="true" />
            <span>{{ tenant.name }}</span>
          </RouterLink>
          <p v-if="props.tenants.length === 0" class="message">{{ t('states.noTenant') }}</p>
        </div>
      </section>
    </div>

    <div class="workspace__sidebar-action">
      <BaseButton class="workspace__sidebar-action-button" variant="secondary" type="button" @click="emit('logout')">
        <IconLogout :size="24" stroke-width="1.5" aria-hidden="true" />
        <span class="workspace__sidebar-action-label">{{ t('nav.logout') }}</span>
      </BaseButton>
    </div>

    <div class="workspace__sidebar-footer">
      <BaseButton
        class="workspace__sidebar-footer-button workspace__sidebar-footer-button--icon"
        variant="secondary"
        type="button"
        :aria-label="props.isCollapsed ? t('actions.expandSidebar') : t('actions.collapseSidebar')"
        :title="props.isCollapsed ? t('actions.expandSidebar') : t('actions.collapseSidebar')"
        @click="emit('toggleSidebar')"
      >
        <IconLayoutSidebarLeftExpand v-if="props.isCollapsed" :size="24" stroke-width="1.5" aria-hidden="true" />
        <IconLayoutSidebarLeftCollapse v-else :size="24" stroke-width="1.5" aria-hidden="true" />
      </BaseButton>
    </div>
  </aside>
</template>
