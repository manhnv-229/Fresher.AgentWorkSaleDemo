<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import LoginForm from '../components/auth/LoginForm.vue';
import { useAuth } from '../composables/useAuth';
import { useI18n } from '../i18n';

const { isAuthenticated, isInitializing, initializeAuth } = useAuth();
const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const redirectTarget = computed(() => {
  const redirect = route.query.redirect;
  return typeof redirect === 'string' && redirect.length > 0 ? redirect : null;
});

onMounted(() => {
  void initializeAuth();
});

// Khi đã có session hợp lệ thì redirect sang target hoặc dashboard.
watch(isAuthenticated, (authenticated) => {
  if (authenticated) {
    if (redirectTarget.value) {
      router.replace(redirectTarget.value);
      return;
    }

    router.replace({ name: 'dashboard' });
  }
}, { immediate: true });
</script>

<template>
  <main class="auth-shell">
    <p v-if="isInitializing" class="message">{{ t('auth.checkingSession') }}</p>
    <LoginForm v-else />
  </main>
</template>
