<script setup lang="ts">
import { computed } from 'vue';
import { RouterLink } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';
import {
  IconClock,
  IconLayoutSidebarLeftCollapse,
  IconLayoutSidebarLeftExpand,
  IconPasswordUser,
  IconUserPlus,
  IconHistory
} from '@tabler/icons-vue';

const props = defineProps<{
  activeRouteName: string | symbol | null | undefined;
  isCollapsed: boolean;
}>();

const emit = defineEmits<{
  toggleSidebar: [];
}>();

const items = computed(() => [
  {
    key: 'settings-members',
    label: 'Quản lý thành viên',
    to: { name: 'settings-members' as const },
    isActive: props.activeRouteName === 'settings' || props.activeRouteName === 'settings-members',
    icon: IconUserPlus
  },
  {
    key: 'settings-password',
    label: 'Đổi mật khẩu',
    to: { name: 'settings-password' as const },
    isActive: props.activeRouteName === 'settings-password',
    icon: IconPasswordUser
  },
  {
    key: 'settings-audit-log',
    label: 'Nhật ký hoạt động',
    to: { name: 'settings-audit-log' as const },
    isActive: props.activeRouteName === 'settings-audit-log',
    icon: IconHistory
  }
]);
</script>

<template>
  <aside class="workspace__settings-sidebar" :class="{ 'workspace__settings-sidebar--collapsed': props.isCollapsed }">
    <div class="workspace__settings-sidebar-content">
      <nav class="settings-nav" aria-label="Thiết lập">
        <div v-for="item in items" :key="item.key" class="sidebar__item" :class="{ 'sidebar__item--collapsed': props.isCollapsed }">
          <RouterLink
            class="scope-link"
            :class="{ 'scope-link--active': item.isActive }"
            :to="item.to"
          >
            <component :is="item.icon" :size="20" stroke-width="1.5" aria-hidden="true" />
            <span>{{ item.label }}</span>
          </RouterLink>
        </div>
      </nav>
    </div>

    <div class="workspace__settings-sidebar-footer">
      <BaseButton
        class="workspace__settings-sidebar-footer-button"
        variant="secondary"
        type="button"
        :aria-label="props.isCollapsed ? 'Mở rộng sidebar thiết lập' : 'Thu gọn sidebar thiết lập'"
        :title="props.isCollapsed ? 'Mở rộng sidebar thiết lập' : 'Thu gọn sidebar thiết lập'"
        @click="emit('toggleSidebar')"
      >
        <IconLayoutSidebarLeftExpand v-if="props.isCollapsed" :size="24" stroke-width="1.5" aria-hidden="true" />
        <IconLayoutSidebarLeftCollapse v-else :size="24" stroke-width="1.5" aria-hidden="true" />
      </BaseButton>
    </div>
  </aside>
</template>
