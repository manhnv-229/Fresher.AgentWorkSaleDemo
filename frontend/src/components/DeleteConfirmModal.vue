<script setup lang="ts">
import BaseButton from './BaseButton.vue';
import BaseModal from './BaseModal.vue';

withDefaults(
  defineProps<{
    open: boolean;
    title?: string;
    confirmLabel?: string;
    busy?: boolean;
  }>(),
  {
    title: 'Xác nhận xóa',
    confirmLabel: 'Xác nhận xóa',
    busy: false
  }
);

const emit = defineEmits<{
  close: [];
  confirm: [];
}>();
</script>

<template>
  <BaseModal :open="open" :title="title" @close="emit('close')">
    <div class="delete-confirm">
      <slot />
      <div class="action-bar">
        <BaseButton variant="secondary" type="button" :disabled="busy" @click="emit('close')">Hủy</BaseButton>
        <BaseButton variant="danger" type="button" :disabled="busy" @click="emit('confirm')">
          {{ busy ? 'Đang xóa...' : confirmLabel }}
        </BaseButton>
      </div>
    </div>
  </BaseModal>
</template>
