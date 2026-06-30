<script setup lang="ts">
import { computed, useAttrs } from 'vue';

defineOptions({
  inheritAttrs: false
});

const attrs = useAttrs();
const wrapperClass = computed(() => attrs.class);
const inputAttrs = computed(() => {
  const { class: _class, ...rest } = attrs;
  return rest;
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
    error?: string;
    hint?: string;
  }>(),
  {
    type: 'text',
    disabled: false,
    hasAction: false,
    error: '',
    hint: ''
  }
);
</script>

<template>
  <label
    class="field"
    :class="[wrapperClass, { 'field--with-action': hasAction, 'field--invalid': Boolean(error) }]"
  >
    <span v-if="label" class="sr-only">{{ label }}</span>
    <input
      :id="id"
      v-model="model"
      :type="type"
      :name="name"
      :autocomplete="autocomplete"
      :placeholder="placeholder"
      :disabled="disabled"
      :aria-invalid="error ? 'true' : 'false'"
      :aria-describedby="error ? `${id || name || 'field'}-error` : hint ? `${id || name || 'field'}-hint` : undefined"
      v-bind="inputAttrs"
    />
    <slot name="action" />
    <span v-if="error" :id="`${id || name || 'field'}-error`" class="field__feedback field__feedback--error" role="alert">
      {{ error }}
    </span>
    <span v-else-if="hint" :id="`${id || name || 'field'}-hint`" class="field__feedback">
      {{ hint }}
    </span>
  </label>
</template>
