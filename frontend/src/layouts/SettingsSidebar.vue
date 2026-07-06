<script setup lang="ts">
import { RouterLink } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import {
  IconClock,
  IconLayoutSidebarLeftCollapse,
  IconLayoutSidebarLeftExpand,
  IconPasswordUser,
  IconUserPlus
} from '@tabler/icons-vue';

defineProps<{
  activeRouteName: string | symbol | null | undefined;
  isCollapsed: boolean;
}>();

const emit = defineEmits<{
  toggleSidebar: [];
}>();
</script>

<template>
  <aside class="workspace__settings-sidebar" :class="{ 'workspace__settings-sidebar--collapsed': isCollapsed }">
    <div class="workspace__settings-sidebar-content">
      <nav class="settings-nav" aria-label="Thiết lập">
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': (activeRouteName === 'settings') || (activeRouteName === 'settings-members') }"
          :to="{ name: 'settings-members' }"
        >
          <IconUserPlus :size="18" stroke-width="1.5" aria-hidden="true" />
          <span>Quản lý thành viên</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': activeRouteName === 'settings-password' }"
          :to="{ name: 'settings-password' }"
        >
          <IconPasswordUser :size="18" stroke-width="1.5" aria-hidden="true" />
          <span>Đổi mật khẩu</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': activeRouteName === 'settings-audit-log' }"
          :to="{ name: 'settings-audit-log' }"
        >
          <IconClock :size="18" stroke-width="1.5" aria-hidden="true" />
          <span>Nhật ký hoạt động</span>
        </RouterLink>
      </nav>
    </div>

    <div class="workspace__settings-sidebar-footer">
      <BaseButton
        class="workspace__settings-sidebar-footer-button"
        variant="secondary"
        type="button"
        :aria-label="isCollapsed ? 'Mở rộng sidebar thiết lập' : 'Thu gọn sidebar thiết lập'"
        :title="isCollapsed ? 'Mở rộng sidebar thiết lập' : 'Thu gọn sidebar thiết lập'"
        @click="emit('toggleSidebar')"
      >
        <IconLayoutSidebarLeftExpand v-if="isCollapsed" :size="18" stroke-width="1.5" aria-hidden="true" />
        <IconLayoutSidebarLeftCollapse v-else :size="18" stroke-width="1.5" aria-hidden="true" />
      </BaseButton>
    </div>
  </aside>
</template>
