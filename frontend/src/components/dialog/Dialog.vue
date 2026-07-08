<script setup lang="ts">
import { IconX } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';

withDefaults(
  defineProps<{
    open: boolean;
    title: string;
    description: string;
    confirmLabel?: string;
    cancelLabel?: string;
    confirmVariant?: 'primary' | 'secondary' | 'danger';
    confirmDisabled?: boolean;
    busy?: boolean;
    busyLabel?: string;
  }>(),
  {
    confirmLabel: 'Xác nhận',
    cancelLabel: 'Hủy',
    confirmVariant: 'primary',
    confirmDisabled: false,
    busy: false,
    busyLabel: 'Đang xử lý...'
  }
);

const emit = defineEmits<{
  cancel: [];
  confirm: [];
}>();
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="dialog-backdrop" role="presentation" @click.self="emit('cancel')">
      <section class="dialog" role="dialog" aria-modal="true" :aria-label="title">
        <div class="dialog__header">
          <h2 class="dialog__title">{{ title }}</h2>

          <button type="button" class="dialog__close" aria-label="Đóng" @click="emit('cancel')">
            <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
          </button>
        </div>

        <p class="dialog__description">
          {{ description }}
        </p>

        <div v-if="$slots.default" class="dialog__content">
          <slot />
        </div>

        <div class="dialog__actions">
          <BaseButton variant="secondary" type="button" :disabled="busy" @click="emit('cancel')">
            {{ cancelLabel }}
          </BaseButton>
          <BaseButton :variant="confirmVariant" type="button" :disabled="busy || confirmDisabled" @click="emit('confirm')">
            {{ busy ? busyLabel : confirmLabel }}
          </BaseButton>
        </div>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.dialog-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: rgba(15, 23, 42, 0.32);
}

.dialog {
  position: relative;
  width: fit-content;
  max-width: calc(100vw - 48px);
  padding: 24px;
  border-radius: 12px;
  background: var(--color-surface);
  box-shadow: var(--shadow-dialog);
  min-width: 320px;
}

.dialog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.dialog__close {
  width: 40px;
  height: 40px;
  border: 0;
  padding: 0;
  border-radius: 0;
  background: transparent;
  color: var(--color-text-subtle);
  cursor: pointer;
  display: inline-grid;
  place-items: center;
}

.dialog__close:hover {
  background: transparent;
  color: var(--color-text);
}

.dialog__title {
  margin: 0;
  font-size: var(--font-size-h3);
  line-height: var(--line-height-h3);
  font-weight: 700;
}

.dialog__description {
  margin: 0 0 16px;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  color: var(--color-text);
}

.dialog__content {
  margin: 0 0 24px;
}

.dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
