<script setup lang="ts">
import { RouterLink } from 'vue-router';

defineProps<{
  activeRouteName: string | symbol | null | undefined;
}>();
</script>

<template>
  <aside class="workspace__settings-sidebar">
    <nav class="settings-nav" aria-label="Thiết lập">
      <RouterLink
        class="scope-link"
        :class="{ 'scope-link--active': activeRouteName === 'settings-members' }"
        :to="{ name: 'settings-members' }"
      >
        <i class="ti ti-user-plus" aria-hidden="true"></i>
        Quản lý thành viên
      </RouterLink>
      <RouterLink
        class="scope-link"
        :class="{ 'scope-link--active': activeRouteName === 'settings-password' }"
        :to="{ name: 'settings-password' }"
      >
        Đổi mật khẩu
      </RouterLink>
      <RouterLink
        class="scope-link"
        :class="{ 'scope-link--active': activeRouteName === 'settings-audit-log' }"
        :to="{ name: 'settings-audit-log' }"
      >
        <i class="ti ti-clock" aria-hidden="true"></i>
        Nhật ký hoạt động
      </RouterLink>
    </nav>
  </aside>
</template>
