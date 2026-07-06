<script setup lang="ts">
import BaseButton from './BaseButton.vue';
import BaseModal from './BaseModal.vue';
import AppLeavePageWarning from './AppLeavePageWarning.vue';
import { ref } from 'vue';

const props = withDefaults(
  defineProps<{
    open: boolean;
    title?: string;
    busy?: boolean;
    confirmDisabled?: boolean;
    cancelLabel?: string;
    confirmLabel?: string;
    busyLabel?: string;
    confirmVariant?: 'primary' | 'secondary' | 'danger';
    hasUnsavedChanges?: boolean;
  }>(),
  {
    title: '',
    busy: false,
    confirmDisabled: false,
    cancelLabel: 'Hủy',
    confirmLabel: 'Lưu',
    busyLabel: 'Đang xử lý...',
    confirmVariant: 'primary',
    hasUnsavedChanges: false
  }
);

const emit = defineEmits<{
  close: [];
  confirm: [];
}>();

const isLeavePageWarningOpen = ref(false);

function handleCloseRequest() {
  if (props.hasUnsavedChanges && !props.busy) {
    isLeavePageWarningOpen.value = true;
    return;
  }

  emit('close');
}
</script>

<template>
  <BaseModal :open="open" :title="title" @close="handleCloseRequest">
    <div class="modal-action-shell">
      <slot />
      <div class="action-bar">
        <BaseButton variant="secondary" type="button" :disabled="busy" @click="handleCloseRequest">
          {{ cancelLabel }}
        </BaseButton>
        <BaseButton :variant="confirmVariant" type="button" :disabled="busy || confirmDisabled" @click="emit('confirm')">
          {{ busy ? busyLabel : confirmLabel }}
        </BaseButton>
      </div>
    </div>
  </BaseModal>
  <AppLeavePageWarning
    :open="isLeavePageWarningOpen"
    @stay="isLeavePageWarningOpen = false"
    @discard="isLeavePageWarningOpen = false; emit('close')"
  />
</template>
