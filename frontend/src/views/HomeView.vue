<script setup lang="ts">
import { LogOut } from '@lucide/vue';
import BaseButton from '../components/ui/BaseButton.vue';
import LoginForm from '../features/auth/components/LoginForm.vue';
import { useAuth } from '../features/auth';
import { formatDate } from '../utils/formatDate';

const { authState, isAuthenticated, accessTokenPreview, logout } = useAuth();
</script>

<template>
  <main class="app-shell">
    <LoginForm v-if="!isAuthenticated" />

    <section v-else class="authenticated-panel" aria-labelledby="auth-title">
      <div>
        <p class="eyebrow">Đăng nhập thành công</p>
        <h1 id="auth-title">Phiên demo đã sẵn sàng</h1>
        <p class="token-preview">Access token: {{ accessTokenPreview }}</p>
        <p v-if="authState" class="expires">
          Hết hạn: {{ formatDate(authState.accessTokenExpiresAt) }}
        </p>
      </div>

      <BaseButton variant="secondary" type="button" @click="logout">
        <LogOut :size="18" aria-hidden="true" />
        Đăng xuất
      </BaseButton>
    </section>
  </main>
</template>
