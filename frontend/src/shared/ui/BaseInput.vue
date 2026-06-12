<script setup lang="ts">
const model = defineModel<string>({ required: true });

withDefaults(
  defineProps<{
    id?: string;
    name?: string;
    type?: string;
    placeholder?: string;
    autocomplete?: string;
    disabled?: boolean;
    label?: string;
    hasAction?: boolean;
  }>(),
  {
    type: 'text',
    disabled: false,
    hasAction: false
  }
);
</script>

<template>
  <label class="field" :class="{ 'field--with-action': hasAction }">
    <span v-if="label" class="sr-only">{{ label }}</span>
    <input
      :id="id"
      v-model="model"
      :type="type"
      :name="name"
      :autocomplete="autocomplete"
      :placeholder="placeholder"
      :disabled="disabled"
    />
    <slot name="action" />
  </label>
</template>
