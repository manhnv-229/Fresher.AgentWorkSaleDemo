<script setup lang="ts">
import { Eye, EyeOff } from '../icons/tabler';
import { ref } from 'vue';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import ContentPanel from '../components/ContentPanel.vue';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import { useAuth } from '../composables/useAuth';
import { hasMaxLength, hasMinLength, isRequired } from '../utils/validators';

const { changePassword: submitPasswordChange } = useAuth();

const currentPassword = ref('');
const newPassword = ref('');
const notice = ref('');
const isLoading = ref(false);
const showCurrentPassword = ref(false);
const showNewPassword = ref(false);
const { errors, formError, validate, clearErrors, clearFieldError, applyApiError } = useFormValidation(
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
      } else if (!hasMaxLength(values.currentPassword, 255)) {
        nextErrors.currentPassword = 'Mật khẩu hiện tại không được vượt quá 255 ký tự.';
      }

      if (!isRequired(values.newPassword)) {
        nextErrors.newPassword = 'Vui lòng nhập mật khẩu mới.';
      } else if (!hasMinLength(values.newPassword, 8)) {
        nextErrors.newPassword = 'Mật khẩu mới phải có ít nhất 8 ký tự.';
      } else if (!hasMaxLength(values.newPassword, 255)) {
        nextErrors.newPassword = 'Mật khẩu mới không được vượt quá 255 ký tự.';
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
  notice.value = '';
  clearErrors();
}

async function submit() {
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
    applyApiError(err, {
      invalid_current_password: 'currentPassword',
      validation_error: FORM_ERROR
    }, 'Không thể đổi mật khẩu.');
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <section class="settings-password-page">
    <ContentPanel class="settings-password-card">
      <header class="settings-password-header">
        <h2 class="settings-password-title">Đổi mật khẩu</h2>
        <!-- <p class="settings-password-description">Cập nhật mật khẩu đăng nhập cho tài khoản của bạn.</p> -->
      </header>

      <p v-if="notice" class="message">{{ notice }}</p>

      <form class="settings-password-form" @submit.prevent="submit">
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
              <button
                class="field__action"
                type="button"
                :aria-label="showCurrentPassword ? 'Ẩn mật khẩu hiện tại' : 'Hiện mật khẩu hiện tại'"
                :title="showCurrentPassword ? 'Ẩn mật khẩu hiện tại' : 'Hiện mật khẩu hiện tại'"
                @click="showCurrentPassword = !showCurrentPassword"
              >
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
              <button
                class="field__action"
                type="button"
                :aria-label="showNewPassword ? 'Ẩn mật khẩu mới' : 'Hiện mật khẩu mới'"
                :title="showNewPassword ? 'Ẩn mật khẩu mới' : 'Hiện mật khẩu mới'"
                @click="showNewPassword = !showNewPassword"
              >
                <EyeOff v-if="showNewPassword" :size="16" aria-hidden="true" />
                <Eye v-else :size="16" aria-hidden="true" />
              </button>
            </template>
          </BaseInput>
        </div>

        <p v-if="formError" class="message message--error">{{ formError }}</p>

        <div class="action-bar">
          <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="clearForm">Xóa</BaseButton>
          <BaseButton type="submit" :disabled="isLoading">
            {{ isLoading ? 'Đang cập nhật...' : 'Xác nhận đổi mật khẩu' }}
          </BaseButton>
        </div>
      </form>
    </ContentPanel>
  </section>
</template>

<style scoped>
.settings-password-page {
  display: grid;
  gap: 24px;
}

.settings-password-header {
  display: grid;
  gap: 4px;
}

.settings-password-title {
  margin: 0;
  font-size: var(--font-size-h2);
  line-height: var(--line-height-h2);
  font-weight: 700;
}

.settings-password-description {
  margin: 0;
  color: var(--color-text-subtle);
}

.settings-password-form {
  display: grid;
  gap: 16px;
  margin-top: 16px;
}
</style>
