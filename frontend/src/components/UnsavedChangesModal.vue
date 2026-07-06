<script setup lang="ts">
import BaseButton from './BaseButton.vue';
import BaseModal from './BaseModal.vue';

defineProps<{
  open: boolean;
}>();

const emit = defineEmits<{
  stay: [];
  discard: [];
}>();
</script>

<template>
  <BaseModal :open="open" title="Thoát và không lưu?" @close="emit('stay')">
    <div class="delete-confirm">
      <p>Nếu bạn thoát, các dữ liệu đang nhập liệu sẽ không được lưu lại.</p>
      <div class="action-bar">
        <BaseButton variant="secondary" type="button" @click="emit('stay')">Ở lại</BaseButton>
        <BaseButton type="button" @click="emit('discard')">Thoát, không lưu</BaseButton>
      </div>
    </div>
  </BaseModal>
</template>
