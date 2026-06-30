<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';

const route = useRoute();
const router = useRouter();
const isForbidden = computed(() => route.name === 'forbidden');

function goHome() {
  router.replace({ name: 'agents-internal' });
}
</script>

<template>
  <main class="app-shell">
    <section class="state-card state-card--compact error-state">
      <h2>{{ isForbidden ? 'Không có quyền truy cập' : 'Đã có lỗi xảy ra' }}</h2>
      <p>
        {{ isForbidden
          ? 'Tài khoản hiện tại không có quyền truy cập khu vực này. Hãy quay lại khu vực được cấp quyền hoặc đăng nhập bằng tài khoản khác.'
          : 'Không thể hiển thị nội dung được yêu cầu.' }}
      </p>
      <BaseButton type="button" @click="goHome">Quay về trang chính</BaseButton>
    </section>
  </main>
</template>
