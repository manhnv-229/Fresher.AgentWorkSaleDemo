<script setup lang="ts">
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
        </header>
        <slot />
      </section>
    </div>
  </Teleport>
</template>
