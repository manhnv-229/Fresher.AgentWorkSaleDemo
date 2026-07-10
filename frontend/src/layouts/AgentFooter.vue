<script setup lang="ts">
import BaseButton from '../components/buttons/BaseButton.vue';
import { useI18n } from '../i18n';

defineProps<{
  isEditing: boolean;
  isSaving: boolean;
}>();

const emit = defineEmits<{
  cancelEdit: [];
  saveDraft: [];
  saveAndPublish: [];
}>();

const { t } = useI18n();
</script>

<template>
  <footer class="agent-footer">
    <div class="agent-footer__actions">
      <template v-if="isEditing">
        <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="emit('cancelEdit')">{{ t('footer.cancelEdit') }}</BaseButton>
        <BaseButton variant="secondary" type="button" :disabled="isSaving" @click="emit('saveDraft')">{{ t('footer.saveDraft') }}</BaseButton>
        <BaseButton type="button" :disabled="isSaving" @click="emit('saveAndPublish')">
          {{ isSaving ? t('footer.saving') : t('footer.savePublish') }}
        </BaseButton>
      </template>
    </div>
  </footer>
</template>
