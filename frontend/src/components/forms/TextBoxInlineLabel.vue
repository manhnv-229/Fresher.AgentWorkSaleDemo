<script setup lang="ts">
import TextBoxTopLabel from './TextBoxTopLabel.vue';

defineOptions({
  inheritAttrs: false
});

const model = defineModel<string>({ required: true });

withDefaults(
  defineProps<{
    label: string;
    id?: string;
    name?: string;
    type?: string;
    placeholder?: string;
    autocomplete?: string;
    disabled?: boolean;
    labelPosition?: 'hidden' | 'top' | 'none';
    required?: boolean;
    hasAction?: boolean;
    clearable?: boolean;
    error?: string;
    hint?: string;
  }>(),
  {
    type: 'text',
    disabled: false,
    labelPosition: 'top',
    required: false,
    hasAction: false,
    clearable: false,
    error: '',
    hint: ''
  }
);
</script>

<template>
  <div class="field-inline">
    <span class="field-inline__label">
      {{ label }}
      <span v-if="required" class="field-inline__required" aria-hidden="true">*</span>
    </span>

    <TextBoxTopLabel
      v-model="model"
      label-position="none"
      v-bind="$attrs"
      :id="id"
      :name="name"
      :type="type"
      :placeholder="placeholder"
      :autocomplete="autocomplete"
      :disabled="disabled"
      :label="label"
      :required="required"
      :has-action="hasAction"
      :clearable="clearable"
      :error="error"
      :hint="hint"
    />
  </div>
</template>

<style scoped>
.field-inline {
  display: flex;
  align-items: center;
  gap: 8px;
}

.field-inline__label {
  flex: 0 0 auto;
  color: var(--color-text);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
  white-space: nowrap;
}

.field-inline__required {
  margin-left: 2px;
}
</style>
