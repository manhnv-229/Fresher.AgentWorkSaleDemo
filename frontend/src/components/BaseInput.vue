<script setup lang="ts">
import { X } from '@lucide/vue';
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
const inputId = computed(() => attrs.id as string | undefined);

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

const emit = defineEmits<{
  clear: [];
}>();

function clearValue() {
  if (model.value === '') {
    return;
  }

  model.value = '';
  emit('clear');
}
</script>

<template>
  <label
    class="field"
    :class="[wrapperClass, { 'field--with-action': hasAction, 'field--clearable': clearable, 'field--invalid': Boolean(error) }]"
  >
    <span v-if="label" class="sr-only">{{ label }}</span>
    <div class="field__control">
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
      <button
        v-if="clearable && model && model.trim()"
        type="button"
        class="field__action field__action--clear"
        :aria-label="`Xóa ${label || inputId || name || 'nội dung tìm kiếm'}`"
        title="Xóa nội dung"
        :disabled="disabled"
        @mousedown.prevent
        @click="clearValue"
      >
        <X :size="14" aria-hidden="true" />
      </button>
    </div>
    <span v-if="error" :id="`${id || name || 'field'}-error`" class="field__feedback field__feedback--error" role="alert">
      {{ error }}
    </span>
    <span v-else-if="hint" :id="`${id || name || 'field'}-hint`" class="field__feedback">
      {{ hint }}
    </span>
  </label>
</template>
