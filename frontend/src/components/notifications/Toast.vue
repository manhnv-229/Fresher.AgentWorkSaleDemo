<script setup lang="ts">
import {
  IconAlertCircle,
  IconAlertTriangle,
  IconCircleCheck,
  IconInfoCircle,
  IconX
} from '@tabler/icons-vue';

defineOptions({
  inheritAttrs: false
});

defineProps<{
  tone: 'success' | 'error' | 'warning' | 'info';
  message?: string;
}>();

defineEmits<{
  close: [];
}>();
</script>

<template>
  <article class="toast" :class="`toast--${tone}`" role="status">
    <div class="toast__icon" aria-hidden="true">
      <IconCircleCheck v-if="tone === 'success'" :size="24" stroke-width="1.5" />
      <IconAlertCircle v-else-if="tone === 'error'" :size="24" stroke-width="1.5" />
      <IconAlertTriangle v-else-if="tone === 'warning'" :size="24" stroke-width="1.5" />
      <IconInfoCircle v-else :size="24" stroke-width="1.5" />
    </div>

    <div class="toast__body">
      <p v-if="message" class="toast__message">{{ message }}</p>
    </div>

    <button type="button" class="toast__close" aria-label="Đóng thông báo" @click="$emit('close')">
      <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
    </button>
  </article>
</template>

<style scoped>
.toast {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  width: fit-content;
  max-width: min(var(--toast-max-width), calc(100vw - 32px));
  padding: var(--toast-padding-y) var(--toast-padding-x);
  border-radius: var(--toast-radius);
  color: #ffffff;
  box-shadow: var(--shadow-card);
  pointer-events: auto;
}

.toast__icon {
  display: inline-grid;
  flex: 0 0 20px;
  width: 20px;
  height: 20px;
  place-items: center;
  margin-top: 1px;
}

.toast__body {
  flex: 1;
  min-width: 0;
}

.toast__message {
  margin: 0;
}

.toast__message {
  opacity: 0.92;
  font-weight: 600;
}

.toast__close {
  display: inline-grid;
  flex: 0 0 20px;
  width: 20px;
  height: 20px;
  place-items: center;
  margin-left: 4px;
  padding: 0;
  border: 0;
  border-radius: 6px;
  background: transparent;
  color: inherit;
  cursor: pointer;
}

.toast__close:hover {
  background: rgba(255, 255, 255, 0.18);
}

.toast--success {
  background: var(--color-success);
}

.toast--error {
  background: var(--color-danger);
}

.toast--warning {
  background: #d97706;
}

.toast--info {
  background: var(--color-info);
}
</style>
