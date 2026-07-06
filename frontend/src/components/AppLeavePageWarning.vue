<script setup lang="ts">
import { IconX } from '@tabler/icons-vue';
import BaseButton from './BaseButton.vue';

defineProps<{
  open: boolean;
}>();

const emit = defineEmits<{
  stay: [];
  discard: [];
}>();
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="unsaved-dialog-backdrop" role="presentation" @click.self="emit('stay')">
      <section
        class="unsaved-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="unsaved-dialog-title"
      >
        <button type="button" class="unsaved-dialog__close" aria-label="Đóng" @click="emit('stay')">
          <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
        </button>

        <h2 id="unsaved-dialog-title" class="unsaved-dialog__title">Thoát và không lưu?</h2>

        <p class="unsaved-dialog__description">
          Nếu bạn thoát, các dữ liệu đang nhập liệu sẽ không được lưu lại.
        </p>

        <div class="unsaved-dialog__actions">
          <BaseButton class="unsaved-dialog__button" variant="secondary" type="button" @click="emit('stay')">
            Ở lại
          </BaseButton>
          <BaseButton class="unsaved-dialog__button" variant="danger" type="button" @click="emit('discard')">
            Thoát, không lưu
          </BaseButton>
        </div>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.unsaved-dialog-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: rgba(17, 24, 39, 0.18);
}

.unsaved-dialog {
  position: relative;
  width: fit-content;
  max-width: calc(100vw - 48px);
  padding: 24px;
  border-radius: 12px;
  background: var(--color-surface);
  box-shadow: var(--shadow-dialog);
}

.unsaved-dialog__close {
  position: absolute;
  top: 24px;
  right: 24px;
  width: 40px;
  height: 40px;
  border: 0;
  padding: 0;
  border-radius: 999px;
  background: transparent;
  color: var(--color-text-subtle);
  cursor: pointer;
  display: inline-grid;
  place-items: center;
}

.unsaved-dialog__close:hover {
  background: rgba(15, 23, 42, 0.06);
  color: var(--color-text);
}

.unsaved-dialog__title {
  margin: 24px 0 16px;
  font-size: var(--font-size-h3);
  line-height: var(--line-height-h3);
  font-weight: 700;
  color: var(--color-text);
}

.unsaved-dialog__description {
  margin: 0 0 16px;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  color: var(--color-text);
}

.unsaved-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding-top: 0;
}

</style>
