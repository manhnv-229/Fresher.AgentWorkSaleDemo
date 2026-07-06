<script setup lang="ts">
import { IconX } from '@tabler/icons-vue';

defineProps<{
  open: boolean;
  title?: string;
}>();

const emit = defineEmits<{
  close: [];
}>();
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="modal-backdrop" role="presentation" @click.self="emit('close')">
      <section class="modal" role="dialog" aria-modal="true" :aria-label="title">
        <header v-if="title" class="modal__header">
          <h2>{{ title }}</h2>
          <button type="button" class="modal__close" aria-label="Đóng" @click="emit('close')">
            <IconX :size="20" stroke-width="1.5" aria-hidden="true" />
          </button>
        </header>
        <slot />
      </section>
    </div>
  </Teleport>
</template>
