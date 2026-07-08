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
  title: string;
  message?: string;
}>();

defineEmits<{
  close: [];
}>();
</script>

<template>
  <article class="notification" :class="`notification--${tone}`" role="status">
    <div class="notification__icon-wrap" aria-hidden="true">
      <IconCircleCheck v-if="tone === 'success'" :size="20" stroke-width="1.5" class="notification__icon" />
      <IconAlertCircle v-else-if="tone === 'error'" :size="20" stroke-width="1.5" class="notification__icon" />
      <IconAlertTriangle v-else-if="tone === 'warning'" :size="20" stroke-width="1.5" class="notification__icon" />
      <IconInfoCircle v-else :size="20" stroke-width="1.5" class="notification__icon" />
    </div>

    <div class="notification__body">
      <p class="notification__title">{{ title }}</p>
      <p v-if="message" class="notification__message">{{ message }}</p>
      <div v-if="$slots.action" class="notification__action-wrap">
        <slot name="action" />
      </div>
    </div>

    <button type="button" class="notification__close" aria-label="Đóng thông báo" @click="$emit('close')">
      <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
    </button>
  </article>
</template>

<style scoped>
.notification {
  display: grid;
  grid-template-columns: 32px minmax(0, 1fr) 24px;
  gap: 12px;
  width: fit-content;
  max-width: min(var(--notification-max-width), calc(100vw - 32px));
  padding: 16px;
  border-radius: var(--notification-radius);
  background: var(--color-surface);
  color: var(--color-text);
  box-shadow: var(--shadow-surface);
  pointer-events: auto;
}

.notification__icon-wrap {
  width: 32px;
  height: 32px;
  border-radius: 999px;
  display: inline-grid;
  place-items: center;
  flex: 0 0 32px;
  margin-top: 2px;
}

.notification__icon {
  width: 20px;
  height: 20px;
}

.notification__body {
  min-width: 0;
}

.notification__title,
.notification__message {
  margin: 0;
}

.notification__title {
  font-size: var(--font-size-h3);
  line-height: var(--line-height-h3);
  font-weight: 700;
}

.notification__message {
  margin-top: 4px;
  color: var(--color-text-subtle);
}

.notification__action-wrap {
  margin-top: 10px;
}

.notification__close {
  display: inline-grid;
  width: 24px;
  height: 24px;
  place-items: center;
  padding: 0;
  border: 0;
  border-radius: 6px;
  background: transparent;
  color: var(--color-text-subtle);
  cursor: pointer;
}

.notification__close:hover {
  background: rgba(15, 23, 42, 0.06);
  color: var(--color-text);
}

.notification--success .notification__icon-wrap {
  background: var(--color-success-soft);
  color: var(--color-success);
}

.notification--error .notification__icon-wrap {
  background: var(--color-danger-soft);
  color: var(--color-danger);
}

.notification--warning .notification__icon-wrap {
  background: var(--color-warning-soft);
  color: var(--color-warning);
}

.notification--info .notification__icon-wrap {
  background: var(--color-info-soft);
  color: var(--color-info);
}
</style>
