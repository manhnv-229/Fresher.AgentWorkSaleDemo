<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { useToastStore, type ToastItem } from '../stores/useToastStore';
import AppToast from './AppToast.vue';

const toastStore = useToastStore();
const { visibleToasts } = storeToRefs(toastStore);
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
      />
    </div>
  </Teleport>
</template>
