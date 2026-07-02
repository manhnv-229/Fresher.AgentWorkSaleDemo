<script setup lang="ts">
import BaseButton from '../components/BaseButton.vue';

defineProps<{
  isEditing: boolean;
  isSaving: boolean;
}>();

const emit = defineEmits<{
  cancelEdit: [];
  saveDraft: [];
  saveAndPublish: [];
}>();
</script>

<template>
  <footer class="agent-footer">
    <div class="agent-footer__actions">
      <template v-if="isEditing">
        <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="emit('cancelEdit')">Hủy</BaseButton>
        <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="emit('saveDraft')">Lưu nháp</BaseButton>
        <BaseButton type="button" :disabled="isSaving" @click="emit('saveAndPublish')">
          {{ isSaving ? 'Đang lưu...' : 'Lưu và Phát hành' }}
        </BaseButton>
      </template>
    </div>
  </footer>
</template>
