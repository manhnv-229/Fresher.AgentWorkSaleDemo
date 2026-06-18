<script setup lang="ts">
import { onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import LoginForm from '../components/auth/LoginForm.vue';
import { useAuth } from '../composables/useAuth';

const { isAuthenticated, isInitializing, initializeAuth } = useAuth();
const router = useRouter();

onMounted(() => {
  void initializeAuth();
});

watch(isAuthenticated, (authenticated) => {
  if (authenticated) {
    router.replace({ name: 'agents-internal' });
  }
}, { immediate: true });
</script>

<template>
  <main class="auth-shell">
    <p v-if="isInitializing" class="message">Đang kiểm tra phiên đăng nhập...</p>
    <LoginForm v-else />
  </main>
</template>
