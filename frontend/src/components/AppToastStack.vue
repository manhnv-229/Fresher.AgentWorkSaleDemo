<script setup lang="ts">
import { AlertCircle, AlertTriangle, CircleCheck, InfoCircle, X } from '../icons/tabler';
import { storeToRefs } from 'pinia';
import { computed } from 'vue';
import { useToastStore, type ToastItem } from '../stores/useToastStore';

const toastStore = useToastStore();
const { visibleToasts } = storeToRefs(toastStore);

const iconByTone = computed<Record<ToastItem['tone'], typeof CircleCheck>>(() => ({
  success: CircleCheck,
  error: AlertCircle,
  warning: AlertTriangle,
  info: InfoCircle
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
        <component :is="iconByTone[toast.tone]" :size="20" aria-hidden="true" />
        <div class="toast__body">
          <p class="toast__title">{{ toast.title }}</p>
          <p v-if="toast.message" class="toast__message">{{ toast.message }}</p>
        </div>
        <button type="button" class="toast__close" aria-label="Đóng thông báo" @click="toastStore.remove(toast.id)">
          <X :size="16" aria-hidden="true" />
        </button>
      </article>
    </div>
  </Teleport>
</template>
