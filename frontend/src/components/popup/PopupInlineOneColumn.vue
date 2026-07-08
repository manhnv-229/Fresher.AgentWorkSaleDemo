<script setup lang="ts">
import { onBeforeUnmount, onMounted } from 'vue';
import { IconX } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';

withDefaults(
  defineProps<{
    open: boolean;
    title: string;
    description?: string;
    cancelLabel?: string;
    confirmLabel?: string;
    confirmVariant?:
      | 'brand'
      | 'primary'
      | 'danger'
      | 'info'
      | 'warning'
      | 'success'
      | 'neutral'
      | 'neutralInverse'
      | 'secondary';
  }>(),
  {
    description: '',
    cancelLabel: 'Hủy',
    confirmLabel: 'Lưu',
    confirmVariant: 'brand'
  }
);

const emit = defineEmits<{
  cancel: [];
  confirm: [];
  close: [];
}>();

function handleBackdropClick() {
  emit('cancel');
  emit('close');
}

function handleCancel() {
  emit('cancel');
}

function handleConfirm() {
  emit('confirm');
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key !== 'Escape') {
    return;
  }

  event.preventDefault();
  handleBackdropClick();
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown);
});

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown);
});
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="popup-overlay" role="presentation" @click.self="handleBackdropClick">
      <section class="popup" role="dialog" aria-modal="true" :aria-label="title">
        <header class="popup__header">
          <h2 class="popup__title">{{ title }}</h2>

          <button
            type="button"
            class="popup__close"
            aria-label="Đóng popup"
            title="Đóng"
            @click="handleBackdropClick"
          >
            <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
          </button>
        </header>

        <form class="popup__form" @submit.prevent="handleConfirm">
          <div class="popup__body">
            <slot />
          </div>

          <footer class="popup__footer">
            <BaseButton variant="secondary" type="button" @click="handleCancel">
              {{ cancelLabel }}
            </BaseButton>

            <BaseButton :variant="confirmVariant" type="submit">
              {{ confirmLabel }}
            </BaseButton>
          </footer>
        </form>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.popup-overlay {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: rgba(24, 24, 27, 0.42);
}

.popup {
  width: fit-content;
  max-width: calc(100vw - 48px);
  overflow: visible;
  border: 1px solid rgba(255, 255, 255, 0.7);
  border-radius: 12px;
  background: var(--color-surface);
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.08), 0 16px 40px rgba(15, 23, 42, 0.16);
}

.popup__header {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 0 24px;
}

.popup__title {
  min-width: 0;
  margin: 0;
  overflow: hidden;
  color: var(--color-text);
  font-size: 16px;
  line-height: 1.4;
  font-weight: 600;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.popup__close {
  width: 32px;
  height: 32px;
  flex: 0 0 32px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 0;
  border-radius: 8px;
  padding: 0;
  background: transparent;
  color: var(--color-text-subtle);
  cursor: pointer;
}

.popup__close:hover {
  background: var(--color-surface-muted);
  color: var(--color-text);
}

.popup__body {
  padding: 0 24px 16px;
  overflow: visible;
}

.popup__footer {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  padding: 0 24px;
  border-top: 1px solid var(--color-border);
  border-radius: 0 0 12px 12px;
  background: var(--color-surface-muted);
}

.popup__body :deep(.popup-form) {
  display: grid;
  grid-template-columns: max-content minmax(0, 1fr);
  column-gap: 8px;
  row-gap: 12px;
  align-items: center;
  width: 100%;
}

.popup__body :deep(.popup-form__row) {
  display: contents;
}

.popup__body :deep(.popup-form__label) {
  min-width: 0;
  color: var(--color-text);
  font-size: 13px;
  line-height: 1.4;
  font-weight: 400;
  white-space: nowrap;
}

.popup__body :deep(.popup-form__control) {
  min-width: 0;
}

.popup__body :deep(.control),
.popup__body :deep(.select-control__select),
.popup__body :deep(.split-control),
.popup__body :deep(.field__control),
.popup__body :deep(.date-picker-field__input) {
  width: 100%;
}
</style>
