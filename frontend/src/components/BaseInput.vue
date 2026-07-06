<script setup lang="ts">
import TextField from './TextField.vue';

defineOptions({
  inheritAttrs: false
});

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
    clearable?: boolean;
    error?: string;
    hint?: string;
  }>(),
  {
    type: 'text',
    disabled: false,
    hasAction: false,
    clearable: false,
    error: '',
    hint: ''
  }
);
</script>

<template>
  <TextField
    v-model="model"
    label-position="hidden"
    v-bind="$attrs"
    :id="id"
    :name="name"
    :type="type"
    :placeholder="placeholder"
    :autocomplete="autocomplete"
    :disabled="disabled"
    :label="label"
    :has-action="hasAction"
    :clearable="clearable"
    :error="error"
    :hint="hint"
  >
    <template #action>
      <slot name="action" />
    </template>
  </TextField>
</template>
