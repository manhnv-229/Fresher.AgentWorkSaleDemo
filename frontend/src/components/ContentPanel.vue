<script setup lang="ts">
withDefaults(
  defineProps<{
    withPagination?: boolean;
  }>(),
  {
    withPagination: false
  }
);
</script>

<template>
  <div class="content-panel" :class="{ 'content-panel--with-pagination': withPagination }">
    <slot />
  </div>
</template>
