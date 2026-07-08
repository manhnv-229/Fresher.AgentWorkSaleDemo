<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { IconCheck, IconMinus } from '@tabler/icons-vue';

const props = withDefaults(
  defineProps<{
    id?: string;
    name?: string;
    label?: string;
    disabled?: boolean;
    error?: string;
    indeterminate?: boolean;
  }>(),
  {
    id: undefined,
    name: undefined,
    label: '',
    disabled: false,
    error: '',
    indeterminate: false
  }
);

const model = defineModel<boolean>({ default: false });
const inputRef = ref<HTMLInputElement | null>(null);

const isInvalid = computed(() => Boolean(props.error));
const isSelected = computed(() => Boolean(model.value));
const isIndeterminate = computed(() => Boolean(props.indeterminate) && !isSelected.value);
const wrapperClass = computed(() => ({
  'choice--selected': isSelected.value,
  'choice--indeterminate': isIndeterminate.value,
  'choice--error': isInvalid.value,
  'choice--disabled': props.disabled
}));

watch(
  [isIndeterminate, isSelected],
  () => {
    if (inputRef.value) {
      inputRef.value.indeterminate = isIndeterminate.value;
    }
  },
  { immediate: true }
);
</script>

<template>
  <label class="choice choice--checkbox" :class="wrapperClass">
    <input
      :id="id"
      ref="inputRef"
      v-model="model"
      class="choice__input"
      type="checkbox"
      :name="name"
      :disabled="disabled"
      :aria-invalid="isInvalid ? 'true' : 'false'"
    />
    <span class="choice__control" aria-hidden="true">
      <IconMinus
        v-if="isIndeterminate"
        class="choice__icon"
        :size="12"
        stroke-width="1.5"
      />
      <IconCheck
        v-else-if="isSelected"
        class="choice__icon"
        :size="12"
        stroke-width="1.5"
      />
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
  border-radius: 4px;
  background: var(--color-surface);
  color: var(--color-surface);
  transition:
    border-color 120ms ease,
    background-color 120ms ease,
    box-shadow 120ms ease,
    color 120ms ease;
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

.choice--selected .choice__control,
.choice--indeterminate .choice__control {
  border-color: var(--color-brand);
  background: var(--color-brand);
  color: #ffffff;
}

.choice--error .choice__control {
  border-color: var(--color-danger);
}

.choice--error.choice--selected .choice__control,
.choice--error.choice--indeterminate .choice__control {
  background: var(--color-danger);
  color: #ffffff;
}

.choice--disabled .choice__control {
  border-color: var(--color-border);
  background: var(--color-surface-muted);
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

.choice__icon {
  flex: 0 0 auto;
}
</style>
