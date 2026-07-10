<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    id?: string;
    name?: string;
    value: string;
    label?: string;
    disabled?: boolean;
    error?: string;
  }>(),
  {
    id: undefined,
    name: undefined,
    label: '',
    disabled: false,
    error: ''
  }
);

const model = defineModel<string>({ default: '' });

const isInvalid = computed(() => Boolean(props.error));
const isSelected = computed(() => model.value === props.value);
const wrapperClass = computed(() => ({
  'choice--selected': isSelected.value,
  'choice--error': isInvalid.value,
  'choice--disabled': props.disabled
}));

// Radio button chỉ là một state machine mỏng cho lựa chọn đơn.
</script>

<template>
  <label class="choice choice--radio" :class="wrapperClass">
    <input
      :id="id"
      v-model="model"
      class="choice__input"
      type="radio"
      :name="name"
      :value="value"
      :disabled="disabled"
      :aria-invalid="isInvalid ? 'true' : 'false'"
    />
    <span class="choice__control" aria-hidden="true">
      <span v-if="isSelected" class="choice__dot"></span>
    </span>
    <span v-if="label" class="choice__label">{{ label }}</span>
    <span v-if="error" class="choice__error" role="alert">{{ error }}</span>
  </label>
</template>

<style scoped>
.choice {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  cursor: pointer;
}

.choice--disabled {
  cursor: not-allowed;
}

.choice__input {
  position: absolute;
  top: 0;
  left: 0;
  width: 16px;
  height: 16px;
  margin: 0;
  opacity: 0;
}

.choice__control {
  display: inline-flex;
  flex: 0 0 16px;
  width: 16px;
  height: 16px;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--color-border-strong);
  border-radius: 50%;
  background: var(--color-surface);
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    box-shadow 120ms ease;
}

.choice:hover .choice__control {
  border-color: var(--color-brand);
}

.choice__label {
  color: var(--color-text);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
}

.choice__error {
  position: absolute;
  top: calc(100% + 12px);
  left: 0;
  z-index: 2;
  display: inline-flex;
  min-height: 32px;
  align-items: center;
  justify-content: center;
  padding: 0 16px;
  border-radius: 12px;
  background: var(--color-danger);
  color: #ffffff;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transform: translateY(-4px);
  transition:
    opacity 120ms ease,
    transform 120ms ease;
}

.choice__error::before {
  content: "";
  position: absolute;
  top: -6px;
  left: 12px;
  width: 12px;
  height: 12px;
  background: var(--color-danger);
  transform: rotate(45deg);
}

.choice__dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--color-brand);
}

.choice--selected .choice__control {
  border-color: var(--color-brand);
}

.choice--error .choice__control {
  border-color: var(--color-danger);
}

.choice--error .choice__dot {
  background: var(--color-danger);
}

.choice--disabled .choice__control {
  border-color: var(--color-border);
  background: var(--color-surface-muted);
}

.choice--disabled .choice__dot {
  background: var(--color-text-placeholder);
}

.choice--disabled .choice__label {
  color: var(--color-text-placeholder);
}

.choice--error:focus-within .choice__error {
  opacity: 1;
  transform: translateY(0);
}

.choice__input:focus-visible + .choice__control {
  box-shadow: 0 0 0 2px rgba(53, 99, 255, 0.16);
}
</style>
