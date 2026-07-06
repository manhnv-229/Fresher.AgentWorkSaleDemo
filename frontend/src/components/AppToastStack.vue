<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { computed } from 'vue';
import { useToastStore, type ToastItem } from '../stores/useToastStore';
import {
  IconAlertCircle,
  IconAlertTriangle,
  IconCircleCheck,
  IconInfoCircle,
  IconX
} from '@tabler/icons-vue';

const toastStore = useToastStore();
const { visibleToasts } = storeToRefs(toastStore);

const iconByTone = computed<Record<ToastItem['tone'], typeof IconCircleCheck>>(() => ({
  success: IconCircleCheck,
  error: IconAlertCircle,
  warning: IconAlertTriangle,
  info: IconInfoCircle
}));
</script>

<template>
  <Teleport to="body">
    <div v-if="visibleToasts.length > 0" class="toast-stack" aria-live="polite" aria-atomic="false">
      <article
        v-for="toast in visibleToasts"
        :key="toast.id"
        class="toast"
        :class="`toast--${toast.tone}`"
        role="status"
      >
        <component :is="iconByTone[toast.tone]" :size="20" stroke-width="1.5" aria-hidden="true" />
        <div class="toast__body">
          <p class="toast__title">{{ toast.title }}</p>
          <p v-if="toast.message" class="toast__message">{{ toast.message }}</p>
        </div>
        <button type="button" class="toast__close" aria-label="Đóng thông báo" @click="toastStore.remove(toast.id)">
          <IconX :size="16" stroke-width="1.5" aria-hidden="true" />
        </button>
      </article>
    </div>
  </Teleport>
</template>
