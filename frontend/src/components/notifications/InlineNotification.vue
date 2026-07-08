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

withDefaults(
  defineProps<{
    tone: 'success' | 'error' | 'warning' | 'info';
    title: string;
    message: string;
    compact?: boolean;
    closable?: boolean;
  }>(),
  {
    compact: false,
    closable: true
  }
);

defineEmits<{
  close: [];
}>();
</script>

<template>
  <article
    class="inline-notification"
    :class="[
      `inline-notification--${tone}`,
      {
        'inline-notification--compact': compact
      }
    ]"
    role="status"
  >
    <div class="inline-notification__icon-wrap" aria-hidden="true">
      <IconCircleCheck v-if="tone === 'success'" class="inline-notification__icon" :size="20" stroke-width="1.5" />
      <IconAlertCircle v-else-if="tone === 'error'" class="inline-notification__icon" :size="20" stroke-width="1.5" />
      <IconAlertTriangle v-else-if="tone === 'warning'" class="inline-notification__icon" :size="20" stroke-width="1.5" />
      <IconInfoCircle v-else class="inline-notification__icon" :size="20" stroke-width="1.5" />
    </div>

    <div class="inline-notification__content">
      <h3 class="inline-notification__title">{{ title }}</h3>
      <p class="inline-notification__message">{{ message }}</p>
      <div v-if="$slots.action" class="inline-notification__action">
        <slot name="action" />
      </div>
    </div>

    <button
      v-if="closable"
      type="button"
      class="inline-notification__close"
      aria-label="Đóng thông báo"
      @click="$emit('close')"
    >
      <IconX :size="16" stroke-width="1.5" aria-hidden="true" />
    </button>
  </article>
</template>

<style scoped>
.inline-notification {
  display: grid;
  grid-template-columns: 24px minmax(0, 1fr) 24px;
  gap: 12px;
  width: 100%;
  padding: 14px 16px;
  border: 1px solid currentColor;
  border-radius: var(--radius-sm);
  background: var(--color-surface);
  color: var(--color-text);
}

.inline-notification:not(.inline-notification--compact) {
  align-items: start;
}

.inline-notification--compact {
  min-height: 40px;
  align-items: center;
  padding-top: 8px;
  padding-bottom: 8px;
}

.inline-notification__icon-wrap {
  width: 24px;
  height: 24px;
  display: inline-grid;
  place-items: center;
  color: inherit;
  border-radius: 999px;
  align-self: start;
}

.inline-notification__icon {
  width: 20px;
  height: 20px;
}

.inline-notification--compact .inline-notification__icon-wrap {
  margin-top: 0;
  align-self: center;
}

.inline-notification__content {
  min-width: 0;
}

.inline-notification__title,
.inline-notification__message {
  margin: 0;
}

.inline-notification__title {
  font-size: var(--font-size-h3);
  line-height: var(--line-height-h3);
  font-weight: 700;
  color: var(--color-text);
}

.inline-notification__message {
  margin-top: 4px;
  color: var(--color-text);
}

.inline-notification--compact .inline-notification__content {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.inline-notification--compact .inline-notification__title {
  margin: 0;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  white-space: nowrap;
  flex: 0 0 auto;
}

.inline-notification--compact .inline-notification__message {
  margin: 0;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.inline-notification__action {
  margin-top: 10px;
}

.inline-notification__close {
  width: 24px;
  height: 24px;
  border: 0;
  padding: 0;
  border-radius: 6px;
  background: transparent;
  color: var(--color-text-subtle);
  cursor: pointer;
  display: inline-grid;
  place-items: center;
}

.inline-notification__close:hover {
  background: rgba(15, 23, 42, 0.06);
  color: var(--color-text);
}

.inline-notification--success {
  color: var(--color-success);
  background: #effaf3;
}

.inline-notification--success .inline-notification__icon-wrap {
  background: var(--color-success-soft);
  color: var(--color-success);
}

.inline-notification--error {
  color: var(--color-danger);
  background: #fff1f1;
}

.inline-notification--error .inline-notification__icon-wrap {
  background: var(--color-danger-soft);
  color: var(--color-danger);
}

.inline-notification--warning {
  color: #f97316;
  background: #fff8ec;
}

.inline-notification--warning .inline-notification__icon-wrap {
  background: var(--color-warning-soft);
  color: #f97316;
}

.inline-notification--info {
  color: var(--color-info);
  background: #eef6ff;
}

.inline-notification--info .inline-notification__icon-wrap {
  background: var(--color-info-soft);
  color: var(--color-info);
}
</style>
