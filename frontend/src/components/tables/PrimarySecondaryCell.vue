<script setup lang="ts">
defineProps<{
  primary?: string | null;
  secondary?: string | null;
  primaryFallback?: string;
  secondaryFallback?: string;
}>();
</script>

<template>
  <div class="primary-secondary-cell">
    <strong class="primary-secondary-cell__primary">{{ primary || primaryFallback || '—' }}</strong>
    <span class="primary-secondary-cell__secondary">{{ secondary || secondaryFallback || '—' }}</span>
  </div>
</template>

<style scoped>
.primary-secondary-cell {
  display: grid;
  gap: 2px;
  padding-block: 4px;
}

.primary-secondary-cell__primary {
  color: var(--color-text);
  font-size: var(--font-size-body);
  font-weight: 600;
}

.primary-secondary-cell__secondary {
  color: var(--color-text-subtle);
}
</style>
