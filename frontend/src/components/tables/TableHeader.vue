<script setup lang="ts">
defineProps<{
  columns: string[];
}>();
</script>

<template>
  <thead>
    <tr>
      <th v-for="column in columns" :key="column">
        {{ column }}
      </th>
    </tr>
  </thead>
</template>
