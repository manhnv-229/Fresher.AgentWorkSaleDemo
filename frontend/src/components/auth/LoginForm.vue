<script setup lang="ts">
import { ref } from 'vue';
import BaseButton from '../buttons/BaseButton.vue';
import TextBoxTopLabel from '../forms/TextBoxTopLabel.vue';
import { FORM_ERROR, useFormValidation } from '../../composables/useFormValidation';
import { useAuth } from '../../composables/useAuth';
import { isEmail, isRequired } from '../../utils/validators';
import { IconEye, IconEyeOff } from '@tabler/icons-vue';

const email = ref('');
const password = ref('');
const showPassword = ref(false);
const isLoading = ref(false);
const { login } = useAuth();
const { errors, formError, validate, clearErrors, clearFieldError, applyApiError } = useFormValidation(
  {
    get email() {
      return email.value;
    },
    get password() {
      return password.value;
    }
  },
  [
    (values) => {
      const nextErrors: Partial<Record<'email' | 'password', string>> = {};

      if (!isRequired(values.email)) {
        nextErrors.email = 'Vui lòng nhập email.';
      } else if (!isEmail(values.email)) {
        nextErrors.email = 'Email không đúng định dạng.';
      }

      if (!isRequired(values.password)) {
        nextErrors.password = 'Vui lòng nhập mật khẩu.';
      }

      return nextErrors;
    }
  ]
);

async function submitLogin() {
  clearErrors();

  const trimmedEmail = email.value.trim();
  email.value = trimmedEmail;
  if (!validate()) {
    return;
  }

  isLoading.value = true;
  try {
    await login(trimmedEmail, password.value);
    // Không giữ mật khẩu trong state local sau khi đăng nhập thành công.
    password.value = '';
  } catch (error) {
    applyApiError(error, {
      invalid_credentials: 'password',
      locked_account: 'password',
      validation_error: FORM_ERROR
    }, 'Đăng nhập thất bại.');
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <section class="login-panel" aria-labelledby="login-title">
    <h1 id="login-title" class="sr-only">Đăng nhập</h1>

    <form class="login-form" novalidate @submit.prevent="submitLogin">
      <TextBoxTopLabel
        v-model="email"
        id="login-email"
        label-position="hidden"
        name="email"
        autocomplete="username"
        placeholder="Email"
        label="Email"
        :disabled="isLoading"
        :error="errors.email"
        @input="clearFieldError('email')"
      />

      <TextBoxTopLabel
        v-model="password"
        id="login-password"
        label-position="hidden"
        :type="showPassword ? 'text' : 'password'"
        name="password"
        autocomplete="current-password"
        placeholder="Mật khẩu"
        label="Mật khẩu"
        :disabled="isLoading"
        has-action
        :error="errors.password"
        @input="clearFieldError('password')"
      >
        <template #action>
          <button
            class="field__action field__action--plain"
            type="button"
            :aria-label="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
            :title="showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
            :disabled="isLoading"
            @click="showPassword = !showPassword"
          >
            <IconEyeOff v-if="showPassword" :size="20" stroke-width="1.5" aria-hidden="true" />
            <IconEye v-else :size="20" stroke-width="1.5" aria-hidden="true" />
          </button>
        </template>
      </TextBoxTopLabel>

      <p v-if="formError" class="message message--error" role="alert">{{ formError }}</p>

      <BaseButton type="submit" :disabled="isLoading">
        {{ isLoading ? 'Đang đăng nhập...' : 'Đăng nhập' }}
      </BaseButton>
    </form>
  </section>
</template>
