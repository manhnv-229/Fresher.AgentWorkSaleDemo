<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import LoginForm from '../components/auth/LoginForm.vue';
import { useAuth } from '../composables/useAuth';

const { isAuthenticated, isInitializing, initializeAuth } = useAuth();
const route = useRoute();
const router = useRouter();
const redirectTarget = computed(() => {
  const redirect = route.query.redirect;
  return typeof redirect === 'string' && redirect.length > 0 ? redirect : null;
});

onMounted(() => {
  void initializeAuth();
});

watch(isAuthenticated, (authenticated) => {
  if (authenticated) {
    if (redirectTarget.value) {
      router.replace(redirectTarget.value);
      return;
    }

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
