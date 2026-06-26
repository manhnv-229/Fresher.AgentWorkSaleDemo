<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/BaseInput.vue';
import { useAuth } from '../composables/useAuth';

const router = useRouter();
const { changePassword: submitPasswordChange } = useAuth();

const currentPassword = ref('');
const newPassword = ref('');
const error = ref('');
const notice = ref('');
const isLoading = ref(false);

function clearForm() {
  currentPassword.value = '';
  newPassword.value = '';
  error.value = '';
}

async function submit() {
  error.value = '';
  notice.value = '';

  if (!currentPassword.value || !newPassword.value) {
    error.value = 'Vui lòng nhập đủ mật khẩu hiện tại và mật khẩu mới.';
    return;
  }

  if (currentPassword.value === newPassword.value) {
    error.value = 'Mật khẩu mới cần khác mật khẩu hiện tại.';
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
    <div class="content-panel settings-form-panel">
      <p v-if="notice" class="message">{{ notice }}</p>
      <form class="create-agent" @submit.prevent="submit">
        <div class="create-agent__group">
          <label class="create-agent__label" for="current-password">Mật khẩu hiện tại</label>
          <BaseInput id="current-password" v-model="currentPassword" type="password" autocomplete="current-password" placeholder="Nhập mật khẩu hiện tại" />
        </div>
        <div class="create-agent__group">
          <label class="create-agent__label" for="new-password">Mật khẩu mới</label>
          <BaseInput id="new-password" v-model="newPassword" type="password" autocomplete="new-password" placeholder="Nhập mật khẩu mới" />
        </div>
        <p v-if="error" class="message message--error">{{ error }}</p>
        <div class="create-agent__actions">
          <BaseButton variant="secondary" type="button" :disabled="isLoading" @click="clearForm">Xóa</BaseButton>
          <BaseButton type="submit" :disabled="isLoading">
            {{ isLoading ? 'Đang cập nhật...' : 'Xác nhận đổi mật khẩu' }}
          </BaseButton>
        </div>
      </form>
    </div>
  </div>
</template>
