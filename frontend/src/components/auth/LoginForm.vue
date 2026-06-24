<script setup lang="ts">
import { ref } from 'vue';
import { Eye, EyeOff } from '@lucide/vue';
import BaseButton from '../BaseButton.vue';
import BaseInput from '../BaseInput.vue';
import { useAuth } from '../../composables/useAuth';
import { isRequired } from '../../utils/validators';

const email = ref('');
const password = ref('');
const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref('');
const { login } = useAuth();

async function submitLogin() {
  errorMessage.value = '';

  const trimmedEmail = email.value.trim();
  if (!isRequired(trimmedEmail) || !isRequired(password.value)) {
    errorMessage.value = 'Vui lòng nhập email và mật khẩu.';
    return;
  }

  isLoading.value = true;
  try {
    await login(trimmedEmail, password.value);
    // Không giữ mật khẩu trong state local sau khi đăng nhập thành công.
    password.value = '';
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'Đăng nhập thất bại.';
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <section class="login-panel" aria-labelledby="login-title">
    <h1 id="login-title" class="sr-only">Đăng nhập</h1>

    <form class="login-form" novalidate @submit.prevent="submitLogin">
      <BaseInput
        v-model="email"
        name="email"
        autocomplete="username"
        placeholder="Email"
        label="Email"
        :disabled="isLoading"
      />

      <BaseInput
        v-model="password"
        :type="showPassword ? 'text' : 'password'"
        name="password"
        autocomplete="current-password"
        placeholder="Mật khẩu"
        label="Mật khẩu"
        :disabled="isLoading"
        has-action
      >
        <template #action>
          <button
            class="field__action"
            type="button"
            :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
            :title="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
            :disabled="isLoading"
            @click="showPassword = !showPassword"
          >
            <EyeOff v-if="showPassword" :size="18" aria-hidden="true" />
            <Eye v-else :size="18" aria-hidden="true" />
          </button>
        </template>
      </BaseInput>

      <p v-if="errorMessage" class="message message--error" role="alert">{{ errorMessage }}</p>

      <BaseButton type="submit" :disabled="isLoading">
        {{ isLoading ? 'Đang đăng nhập...' : 'Đăng nhập' }}
      </BaseButton>
    </form>
  </section>
</template>
