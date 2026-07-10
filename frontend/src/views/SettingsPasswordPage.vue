<script setup lang="ts">
import { ref } from 'vue';
import BaseButton from '../components/buttons/BaseButton.vue';
import TextBoxTopLabel from '../components/forms/TextBoxTopLabel.vue';
import { FORM_ERROR, useFormValidation } from '../composables/useFormValidation';
import { useAuth } from '../composables/useAuth';
import { hasMaxLength, hasMinLength, isRequired } from '../utils/validators';
import { IconEye, IconEyeOff } from '@tabler/icons-vue';
import { useI18n } from '../i18n';

const { changePassword: submitPasswordChange } = useAuth();
const { t } = useI18n();

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
        nextErrors.currentPassword = t('settingsPassword.currentRequired');
      } else if (!hasMaxLength(values.currentPassword, 255)) {
        nextErrors.currentPassword = t('settingsPassword.currentTooLong');
      }

      if (!isRequired(values.newPassword)) {
        nextErrors.newPassword = t('settingsPassword.newRequired');
      } else if (!hasMinLength(values.newPassword, 8)) {
        nextErrors.newPassword = t('settingsPassword.newTooShort');
      } else if (!hasMaxLength(values.newPassword, 255)) {
        nextErrors.newPassword = t('settingsPassword.newTooLong');
      } else if (values.currentPassword === values.newPassword) {
        nextErrors.newPassword = t('settingsPassword.newDifferent');
      }

      return nextErrors;
    }
  ]
);

// Reset toàn bộ form để tránh giữ lại mật khẩu cũ trong state local.
function clearForm() {
  currentPassword.value = '';
  newPassword.value = '';
  notice.value = '';
  clearErrors();
}

// Submit đổi mật khẩu chỉ chạy sau khi validate local pass.
async function submit() {
  notice.value = '';
  clearErrors();
  if (!validate()) {
    return;
  }

  isLoading.value = true;
  try {
    await submitPasswordChange(currentPassword.value, newPassword.value);
    notice.value = t('settingsPassword.updated');
    clearForm();
  } catch (err) {
    applyApiError(err, {
      invalid_current_password: 'currentPassword',
      validation_error: FORM_ERROR
    }, t('settingsPassword.updateFailed'));
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <section class="settings-password-page">
    <div class="content-panel settings-password-card">
      <header class="settings-password-header">
        <h2 class="settings-password-title">{{ t('settings.password') }}</h2>
        <!-- <p class="settings-password-description">Cập nhật mật khẩu đăng nhập cho tài khoản của bạn.</p> -->
      </header>

      <p v-if="notice" class="message">{{ notice }}</p>

      <form id="settings-password-form" class="settings-password-form" @submit.prevent="submit">
        <div class="create-agent__group">
          <label class="create-agent__label" for="current-password">{{ t('settingsPassword.currentLabel') }}</label>
          <TextBoxTopLabel
            id="current-password"
            v-model="currentPassword"
            label-position="hidden"
            :type="showCurrentPassword ? 'text' : 'password'"
            autocomplete="current-password"
            :placeholder="t('settingsPassword.currentPlaceholder')"
            has-action
            :error="errors.currentPassword"
            @input="clearFieldError('currentPassword')"
          >
            <template #action>
              <button
                class="field__action field__action--plain"
                type="button"
                :aria-label="showCurrentPassword ? t('settingsPassword.hideCurrent') : t('settingsPassword.showCurrent')"
                :title="showCurrentPassword ? t('settingsPassword.hideCurrent') : t('settingsPassword.showCurrent')"
                @click="showCurrentPassword = !showCurrentPassword"
              >
                <IconEyeOff v-if="showCurrentPassword" :size="24" stroke-width="1.5" aria-hidden="true" />
                <IconEye v-else :size="24" stroke-width="1.5" aria-hidden="true" />
              </button>
            </template>
          </TextBoxTopLabel>
        </div>

        <div class="create-agent__group">
          <label class="create-agent__label" for="new-password">{{ t('settingsPassword.newLabel') }}</label>
          <TextBoxTopLabel
            id="new-password"
            v-model="newPassword"
            label-position="hidden"
            :type="showNewPassword ? 'text' : 'password'"
            autocomplete="new-password"
            :placeholder="t('settingsPassword.newPlaceholder')"
            has-action
            :error="errors.newPassword"
            @input="clearFieldError('newPassword')"
          >
            <template #action>
              <button
                class="field__action field__action--plain"
                type="button"
                :aria-label="showNewPassword ? t('settingsPassword.hideNew') : t('settingsPassword.showNew')"
                :title="showNewPassword ? t('settingsPassword.hideNew') : t('settingsPassword.showNew')"
                @click="showNewPassword = !showNewPassword"
              >
                <IconEyeOff v-if="showNewPassword" :size="24" stroke-width="1.5" aria-hidden="true" />
                <IconEye v-else :size="24" stroke-width="1.5" aria-hidden="true" />
              </button>
            </template>
          </TextBoxTopLabel>
        </div>
      </form>

      <p v-if="formError" class="message message--error">{{ formError }}</p>

      <div class="action-bar">
        <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="clearForm">{{ t('actions.clear') }}</BaseButton>
        <BaseButton type="submit" form="settings-password-form" :disabled="isLoading">
          {{ isLoading ? t('settingsPassword.updating') : t('settingsPassword.submit') }}
        </BaseButton>
      </div>
    </div>
  </section>
</template>

<style scoped>
.settings-password-page {
  display: grid;
  gap: 24px;
}

.settings-password-card {
  display: flex;
  flex-direction: column;
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

.action-bar {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: auto;
  padding-top: 16px;
}
</style>
