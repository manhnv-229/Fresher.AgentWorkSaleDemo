<script setup lang="ts">
import { storeToRefs } from 'pinia';
import type { Component } from 'vue';
import {
  IconAlertCircle,
  IconAlertTriangle,
  IconCircleCheck,
  IconInfoCircle
} from '@tabler/icons-vue';
import { useToastStore, type ToastItem } from '../stores/useToastStore';
import AppToast from './AppToast.vue';

const toastStore = useToastStore();
const { visibleToasts } = storeToRefs(toastStore);

const toastIconByTone: Record<ToastItem['tone'], Component> = {
  success: IconCircleCheck,
  error: IconAlertCircle,
  warning: IconAlertTriangle,
  info: IconInfoCircle
};
</script>

<template>
  <Teleport to="body">
    <div v-if="visibleToasts.length > 0" class="toast-stack" aria-live="polite" aria-atomic="false">
      <component
        v-for="toast in visibleToasts"
        :key="toast.id"
        :is="AppToast"
        :tone="toast.tone"
        :message="toast.message"
        @close="toastStore.remove(toast.id)"
      >
        <template #icon>
          <component :is="toastIconByTone[toast.tone]" :size="20" stroke-width="1.5" aria-hidden="true" />
        </template>
      </component>
    </div>
  </Teleport>
</template>
