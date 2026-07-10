<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { useToastStore, type ToastItem } from '../../stores/useToastStore';
import Toast from './Toast.vue';

const toastStore = useToastStore();
const { visibleToasts } = storeToRefs(toastStore);

// Stack này chỉ là cổng render toast từ store ra Teleport body.
</script>

<template>
  <Teleport to="body">
    <div v-if="visibleToasts.length > 0" class="toast-stack" aria-live="polite" aria-atomic="false">
      <component
        v-for="toast in visibleToasts"
        :key="toast.id"
        :is="Toast"
        :tone="toast.tone"
        :message="toast.message"
        @close="toastStore.remove(toast.id)"
      />
    </div>
  </Teleport>
</template>
