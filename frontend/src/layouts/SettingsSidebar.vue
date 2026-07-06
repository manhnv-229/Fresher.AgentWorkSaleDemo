<script setup lang="ts">
import { RouterLink } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';

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
          <i class="ti ti-user-plus" aria-hidden="true"></i>
          <span>Quản lý thành viên</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': activeRouteName === 'settings-password' }"
          :to="{ name: 'settings-password' }"
        >
          <i class="ti ti-password-user" aria-hidden="true"></i>
          <span>Đổi mật khẩu</span>
        </RouterLink>
        <RouterLink
          class="scope-link"
          :class="{ 'scope-link--active': activeRouteName === 'settings-audit-log' }"
          :to="{ name: 'settings-audit-log' }"
        >
          <i class="ti ti-clock" aria-hidden="true"></i>
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
        <i v-if="isCollapsed" class="ti ti-layout-sidebar-left-expand" aria-hidden="true"></i>
        <i v-else class="ti ti-layout-sidebar-left-collapse" aria-hidden="true"></i>
      </BaseButton>
    </div>
  </aside>
</template>
