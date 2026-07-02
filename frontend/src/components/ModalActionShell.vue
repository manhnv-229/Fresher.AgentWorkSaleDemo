<script setup lang="ts">
import BaseButton from './BaseButton.vue';
import BaseModal from './BaseModal.vue';

withDefaults(
  defineProps<{
    open: boolean;
    title?: string;
    busy?: boolean;
    confirmDisabled?: boolean;
    cancelLabel?: string;
    confirmLabel?: string;
    busyLabel?: string;
    confirmVariant?: 'primary' | 'secondary' | 'danger';
  }>(),
  {
    title: '',
    busy: false,
    confirmDisabled: false,
    cancelLabel: 'Hủy',
    confirmLabel: 'Lưu',
    busyLabel: 'Đang xử lý...',
    confirmVariant: 'primary'
  }
);

const emit = defineEmits<{
  close: [];
  confirm: [];
}>();
</script>

<template>
  <BaseModal :open="open" :title="title" @close="emit('close')">
    <div class="modal-action-shell">
      <slot />
      <div class="action-bar">
        <BaseButton variant="secondary" type="button" :disabled="busy" @click="emit('close')">
          {{ cancelLabel }}
        </BaseButton>
        <BaseButton :variant="confirmVariant" type="button" :disabled="busy || confirmDisabled" @click="emit('confirm')">
          {{ busy ? busyLabel : confirmLabel }}
        </BaseButton>
      </div>
    </div>
  </BaseModal>
</template>
