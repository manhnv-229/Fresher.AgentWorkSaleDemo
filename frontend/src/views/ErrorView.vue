<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import BaseButton from '../components/buttons/BaseButton.vue';

const route = useRoute();
const router = useRouter();
const isForbidden = computed(() => route.name === 'forbidden');

function goHome() {
  // Quay về dashboard là đường thoát an toàn nhất khi gặp lỗi hoặc thiếu quyền.
  router.replace({ name: 'dashboard' });
}
</script>

<template>
  <main class="app-shell">
    <section class="state-card state-card--compact error-state">
      <h2>{{ isForbidden ? 'Không có quyền truy cập' : 'Đã có lỗi xảy ra' }}</h2>
      <!-- Copy lỗi được tách riêng theo forbidden hay lỗi chung để user biết hướng xử lý tiếp theo. -->
      <p>
        {{ isForbidden
          ? 'Tài khoản hiện tại không có quyền truy cập khu vực này. Hãy quay lại khu vực được cấp quyền hoặc đăng nhập bằng tài khoản khác.'
          : 'Không thể hiển thị nội dung được yêu cầu.' }}
      </p>
      <BaseButton type="button" @click="goHome">Quay về trang chính</BaseButton>
    </section>
  </main>
</template>
