<script setup lang="ts">
import { onBeforeUnmount, onMounted } from 'vue';
import { IconX } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';

withDefaults(
  defineProps<{
    open: boolean;
    title: string;
    description?: string;
    placement?: 'center' | 'anchored';
    width?: string;
    maxWidth?: string;
    minWidth?: string;
    panelStyle?: Record<string, string>;
    cancelLabel?: string;
    confirmLabel?: string;
    showCancel?: boolean;
    showConfirm?: boolean;
    cancelDisabled?: boolean;
    confirmDisabled?: boolean;
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
    placement: 'center',
    width: '',
    maxWidth: 'calc(100vw - 48px)',
    minWidth: '',
    panelStyle: () => ({}),
    cancelLabel: 'Hủy',
    confirmLabel: 'Lưu',
    showCancel: true,
    showConfirm: true,
    cancelDisabled: false,
    confirmDisabled: false,
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
    <div
      v-if="open"
      class="popup-overlay"
      :class="{ 'popup-overlay--anchored': placement === 'anchored' }"
      role="presentation"
      @click.self="handleBackdropClick"
    >
      <section
        class="popup"
        :class="{ 'popup--anchored': placement === 'anchored' }"
        role="dialog"
        aria-modal="true"
        :aria-label="title"
        :style="[
          {
            width: width || undefined,
            minWidth: minWidth || undefined,
            maxWidth
          },
          panelStyle
        ]"
      >
        <header class="popup__header">
          <h2 class="popup__title">{{ title }}</h2>

          <button
            type="button"
            class="popup__close"
            aria-label="Đóng popup"
            title="Đóng"
            @click="handleBackdropClick"
          >
            <IconX :size="24" stroke-width="1.5" aria-hidden="true" />
          </button>
        </header>

        <form class="popup__form" @submit.prevent="handleConfirm">
          <div class="popup__body">
            <slot />
          </div>

          <footer v-if="$slots.footer || showCancel || showConfirm" class="popup__footer">
            <slot name="footer">
              <BaseButton v-if="showCancel" variant="secondary" type="button" :disabled="cancelDisabled" @click="handleCancel">
                {{ cancelLabel }}
              </BaseButton>

              <BaseButton v-if="showConfirm" :variant="confirmVariant" type="submit" :disabled="confirmDisabled">
                {{ confirmLabel }}
              </BaseButton>
            </slot>
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

.popup-overlay--anchored {
  align-items: flex-start;
  justify-content: flex-start;
  padding: 0;
  background: transparent;
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

.popup--anchored {
  box-shadow: var(--shadow-card);
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
  justify-content: space-between;
  gap: 8px;
  padding: 0 24px;
  border-top: 1px solid var(--color-border);
  border-radius: 0 0 12px 12px;
  background: var(--color-surface-muted);
  width: 100%;
}

.popup__body :deep(.popup-form) {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.popup__body :deep(.popup-form__field) {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}

.popup__body :deep(.popup-form__label) {
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

@media (max-width: 640px) {
  .popup-overlay {
    align-items: flex-end;
    padding: 12px;
  }

  .popup {
    width: 100%;
    max-height: calc(100vh - 24px);
    overflow-y: auto;
    min-width: 0;
  }

  .popup__header,
  .popup__footer {
    padding-right: 16px;
    padding-left: 16px;
  }

  .popup__body {
    padding-right: 16px;
    padding-left: 16px;
  }
}
</style>
