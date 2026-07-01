<script setup lang="ts">
import { Eye, EyeOff } from '@lucide/vue';
import { ref } from 'vue';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import ContentPanel from '../components/ContentPanel.vue';
import { useFormValidation } from '../composables/useFormValidation';
import { useAuth } from '../composables/useAuth';
import { hasMinLength, isRequired } from '../utils/validators';

const { changePassword: submitPasswordChange } = useAuth();

const currentPassword = ref('');
const newPassword = ref('');
const error = ref('');
const notice = ref('');
const isLoading = ref(false);
const showCurrentPassword = ref(false);
const showNewPassword = ref(false);
const { errors, validate, clearErrors, clearFieldError } = useFormValidation(
  {
    get currentPassword() {
      return currentPassword.value;
    },
    get newPassword() {
      return newPassword.value;
    }
  },
  [
    (values) => {
      const nextErrors: Partial<Record<'currentPassword' | 'newPassword', string>> = {};

      if (!isRequired(values.currentPassword)) {
        nextErrors.currentPassword = 'Vui lòng nhập mật khẩu hiện tại.';
      }

      if (!isRequired(values.newPassword)) {
        nextErrors.newPassword = 'Vui lòng nhập mật khẩu mới.';
      } else if (!hasMinLength(values.newPassword, 8)) {
        nextErrors.newPassword = 'Mật khẩu mới phải có ít nhất 8 ký tự.';
      } else if (values.currentPassword === values.newPassword) {
        nextErrors.newPassword = 'Mật khẩu mới cần khác mật khẩu hiện tại.';
      }

      return nextErrors;
    }
  ]
);

function clearForm() {
  currentPassword.value = '';
  newPassword.value = '';
  error.value = '';
  notice.value = '';
  clearErrors();
}

async function submit() {
  error.value = '';
  notice.value = '';
  clearErrors();
  if (!validate()) {
    return;
  }

  isLoading.value = true;
  try {
    await submitPasswordChange(currentPassword.value, newPassword.value);
    notice.value = 'Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.';
    clearForm();
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Không thể đổi mật khẩu.';
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="settings-content-card">
    <ContentPanel class="settings-form-panel">
      <p v-if="notice" class="message">{{ notice }}</p>
      <form class="create-agent" @submit.prevent="submit">
        <div class="create-agent__group">
          <label class="create-agent__label" for="current-password">Mật khẩu hiện tại</label>
          <BaseInput
            id="current-password"
            v-model="currentPassword"
            :type="showCurrentPassword ? 'text' : 'password'"
            autocomplete="current-password"
            placeholder="Nhập mật khẩu hiện tại"
            has-action
            :error="errors.currentPassword"
            @input="clearFieldError('currentPassword')"
          >
            <template #action>
              <button class="field__action" type="button" @click="showCurrentPassword = !showCurrentPassword">
                <EyeOff v-if="showCurrentPassword" :size="16" aria-hidden="true" />
                <Eye v-else :size="16" aria-hidden="true" />
              </button>
            </template>
          </BaseInput>
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="new-password">Mật khẩu mới</label>
          <BaseInput
            id="new-password"
            v-model="newPassword"
            :type="showNewPassword ? 'text' : 'password'"
            autocomplete="new-password"
            placeholder="Nhập mật khẩu mới"
            has-action
            :error="errors.newPassword"
            @input="clearFieldError('newPassword')"
          >
            <template #action>
              <button class="field__action" type="button" @click="showNewPassword = !showNewPassword">
                <EyeOff v-if="showNewPassword" :size="16" aria-hidden="true" />
                <Eye v-else :size="16" aria-hidden="true" />
              </button>
            </template>
          </BaseInput>
        </div>
        <p v-if="error" class="message message--error">{{ error }}</p>
        <div class="action-bar">
          <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="clearForm">Xóa</BaseButton>
          <BaseButton type="submit" :disabled="isLoading">
            {{ isLoading ? 'Đang cập nhật...' : 'Xác nhận đổi mật khẩu' }}
          </BaseButton>
        </div>
      </form>
    </ContentPanel>
  </div>
</template>
