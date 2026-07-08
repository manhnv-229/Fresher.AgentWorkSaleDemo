<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, useAttrs } from 'vue';
import { IconChevronDown } from '@tabler/icons-vue';

export type DropdownOption = {
  label: string;
  value: string;
  disabled?: boolean;
};

defineOptions({
  inheritAttrs: false
});

const props = withDefaults(
  defineProps<{
    id?: string;
    name?: string;
    options: DropdownOption[];
    placeholder?: string;
    disabled?: boolean;
    label?: string;
    labelPosition?: 'hidden' | 'top' | 'none';
    required?: boolean;
    error?: string;
    hint?: string;
    ariaLabel?: string;
    state?: 'normal' | 'hover' | 'focus' | 'disabled' | 'error';
  }>(),
  {
    placeholder: 'Chọn giá trị',
    disabled: false,
    labelPosition: 'top',
    required: false,
    error: '',
    hint: '',
    ariaLabel: '',
    state: 'normal'
  }
);

const emit = defineEmits<{
  change: [option: DropdownOption | undefined];
  open: [];
  close: [];
  focus: [event: FocusEvent];
  blur: [event: FocusEvent];
}>();

const model = defineModel<string>({ required: true });
const attrs = useAttrs();
const rootRef = ref<HTMLElement | null>(null);
const buttonRef = ref<HTMLButtonElement | null>(null);
const isOpen = ref(false);
const isFocused = ref(false);
const activeIndex = ref(-1);

const inputId = computed(() => props.id || (attrs.id as string | undefined) || props.name || 'dropdown');
const listboxId = computed(() => `${inputId.value}-listbox`);
const errorId = computed(() => `${inputId.value}-error`);
const hintId = computed(() => `${inputId.value}-hint`);
const hasLabel = computed(() => props.labelPosition !== 'none' && Boolean(props.label));
const wrapperClass = computed(() => attrs.class);
const isDisabled = computed(() => props.disabled || props.state === 'disabled');
const isHoverState = computed(() => props.state === 'hover');
const isFocusState = computed(() => props.state === 'focus');
const isInvalidState = computed(() => Boolean(props.error) || props.state === 'error');
const isVisualFocused = computed(() => isFocused.value || isFocusState.value || isOpen.value);
const selectedOption = computed(() => props.options.find((option) => option.value === model.value));
const hasValue = computed(() => Boolean(selectedOption.value));
const activeOptionId = computed(() => (activeIndex.value >= 0 ? `${inputId.value}-option-${activeIndex.value}` : undefined));
const describedBy = computed(() => {
  if (isInvalidState.value) {
    return errorId.value;
  }

  if (props.hint) {
    return hintId.value;
  }

  return undefined;
});

const fieldClasses = computed(() => [
  wrapperClass.value,
  `field--label-${props.labelPosition}`,
  {
    'field--invalid': isInvalidState.value,
    'field--hover': isHoverState.value,
    'field--focus': isVisualFocused.value,
    'field--disabled': isDisabled.value
  }
]);

function firstEnabledIndex(startIndex = 0) {
  return props.options.findIndex((option, index) => index >= startIndex && !option.disabled);
}

function lastEnabledIndex() {
  for (let index = props.options.length - 1; index >= 0; index -= 1) {
    if (!props.options[index]?.disabled) {
      return index;
    }
  }

  return -1;
}

function nextEnabledIndex(direction: 1 | -1) {
  if (!props.options.length) {
    return -1;
  }

  let index = activeIndex.value;
  for (let offset = 0; offset < props.options.length; offset += 1) {
    index = (index + direction + props.options.length) % props.options.length;
    if (!props.options[index]?.disabled) {
      return index;
    }
  }

  return -1;
}

function setActiveToSelected() {
  const selectedIndex = props.options.findIndex((option) => option.value === model.value && !option.disabled);
  activeIndex.value = selectedIndex >= 0 ? selectedIndex : firstEnabledIndex();
}

function openDropdown() {
  if (isDisabled.value || isOpen.value) {
    return;
  }

  isOpen.value = true;
  setActiveToSelected();
  emit('open');
}

function closeDropdown() {
  if (!isOpen.value) {
    return;
  }

  isOpen.value = false;
  activeIndex.value = -1;
  emit('close');
}

function toggleDropdown() {
  if (isOpen.value) {
    closeDropdown();
    return;
  }

  openDropdown();
}

function selectOption(option: DropdownOption | undefined) {
  if (!option || option.disabled) {
    return;
  }

  model.value = option.value;
  emit('change', option);
  closeDropdown();
  nextTick(() => buttonRef.value?.focus());
}

function handleKeydown(event: KeyboardEvent) {
  if (isDisabled.value) {
    return;
  }

  if (!isOpen.value && ['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
    event.preventDefault();
    openDropdown();
    return;
  }

  if (!isOpen.value) {
    return;
  }

  if (event.key === 'ArrowDown') {
    event.preventDefault();
    activeIndex.value = nextEnabledIndex(1);
  } else if (event.key === 'ArrowUp') {
    event.preventDefault();
    activeIndex.value = nextEnabledIndex(-1);
  } else if (event.key === 'Home') {
    event.preventDefault();
    activeIndex.value = firstEnabledIndex();
  } else if (event.key === 'End') {
    event.preventDefault();
    activeIndex.value = lastEnabledIndex();
  } else if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    selectOption(props.options[activeIndex.value]);
  } else if (event.key === 'Escape') {
    event.preventDefault();
    closeDropdown();
  }
}

function handleFocus(event: FocusEvent) {
  isFocused.value = true;
  emit('focus', event);
}

function handleBlur(event: FocusEvent) {
  isFocused.value = false;
  emit('blur', event);
}

function handleDocumentPointerDown(event: PointerEvent) {
  const target = event.target;

  if (target instanceof Node && rootRef.value?.contains(target)) {
    return;
  }

  closeDropdown();
}

onMounted(() => {
  document.addEventListener('pointerdown', handleDocumentPointerDown);
});

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', handleDocumentPointerDown);
});
</script>

<template>
  <component
    :is="hasLabel ? 'label' : 'div'"
    ref="rootRef"
    class="field dropdown-list"
    :class="fieldClasses"
  >
    <span v-if="hasLabel && labelPosition === 'hidden'" class="sr-only">{{ label }}</span>
    <span v-else-if="hasLabel" class="field__label">
      {{ label }}
      <span v-if="required" class="field__required" aria-hidden="true">*</span>
    </span>

    <div class="field__body">
      <div class="field__control dropdown-list__anchor">
        <button
          :id="inputId"
          ref="buttonRef"
          type="button"
          class="dropdown-list__control"
          :class="{
            'dropdown-list__control--placeholder': !hasValue,
            'dropdown-list__control--selected': hasValue
          }"
          :name="name"
          :disabled="isDisabled"
          :aria-label="!label || labelPosition === 'none' ? (ariaLabel || label || placeholder || undefined) : undefined"
          :aria-invalid="isInvalidState ? 'true' : 'false'"
          :aria-describedby="describedBy"
          :aria-controls="listboxId"
          :aria-expanded="isOpen ? 'true' : 'false'"
          :aria-activedescendant="isOpen ? activeOptionId : undefined"
          aria-haspopup="listbox"
          role="combobox"
          v-bind="{ ...attrs, class: undefined }"
          @click="toggleDropdown"
          @keydown="handleKeydown"
          @focus="handleFocus"
          @blur="handleBlur"
        >
          <span class="dropdown-list__value">
            {{ selectedOption?.label || placeholder }}
          </span>
          <IconChevronDown
            class="dropdown-list__chevron"
            :class="{ 'dropdown-list__chevron--open': isOpen }"
            :size="16"
            stroke-width="1.5"
            aria-hidden="true"
          />
        </button>

        <ul
          v-if="isOpen"
          :id="listboxId"
          class="dropdown-list__menu"
          role="listbox"
          :aria-labelledby="inputId"
        >
          <li
            v-for="(option, index) in options"
            :id="`${inputId}-option-${index}`"
            :key="option.value"
            class="dropdown-list__option"
            :class="{
              'dropdown-list__option--active': index === activeIndex,
              'dropdown-list__option--selected': option.value === model,
              'dropdown-list__option--disabled': option.disabled
            }"
            role="option"
            :aria-selected="option.value === model ? 'true' : 'false'"
            :aria-disabled="option.disabled ? 'true' : undefined"
            @mouseenter="activeIndex = option.disabled ? activeIndex : index"
            @mousedown.prevent
            @click="selectOption(option)"
          >
            {{ option.label }}
          </li>
        </ul>
      </div>

      <span
        v-if="isInvalidState"
        :id="errorId"
        class="field__feedback field__feedback--error"
        role="alert"
      >
        {{ error }}
      </span>
      <span v-else-if="hint" :id="hintId" class="field__feedback">
        {{ hint }}
      </span>
    </div>
  </component>
</template>

<style scoped>
.dropdown-list {
  position: relative;
}

.dropdown-list__anchor {
  position: relative;
  border-radius: var(--field-radius);
  transition: box-shadow 120ms ease;
}

.dropdown-list__anchor:hover,
.field--hover .dropdown-list__anchor,
.field--focus .dropdown-list__anchor,
.dropdown-list__anchor:focus-within {
  box-shadow: 0 0 0 3px rgba(53, 99, 255, 0.08);
}

.dropdown-list__control {
  display: flex;
  width: 100%;
  height: var(--field-height);
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 0 8px 0 var(--field-padding-x);
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  color: var(--color-text);
  font: inherit;
  line-height: var(--line-height-body);
  text-align: left;
  outline: none;
  transition:
    border-color 120ms ease,
    box-shadow 120ms ease,
    background 120ms ease,
    color 120ms ease;
}

.dropdown-list__control:hover:not(:disabled),
.field--hover .dropdown-list__control {
  border-color: var(--color-brand);
}

.dropdown-list__control:focus,
.field--focus .dropdown-list__control {
  border-color: var(--color-brand);
}

.field--invalid .dropdown-list__control {
  border-color: var(--color-danger);
}

.dropdown-list__control:disabled {
  background: var(--color-surface-muted);
  color: var(--color-text-subtle);
}

.dropdown-list__control--placeholder {
  color: var(--color-text-placeholder);
}

.dropdown-list__control--selected {
  color: var(--color-brand);
}

.dropdown-list__value {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dropdown-list__chevron {
  flex: 0 0 16px;
  color: var(--color-text-subtle);
  transition: transform 120ms ease;
}

.dropdown-list__chevron--open {
  transform: rotate(180deg);
}

.dropdown-list__menu {
  position: absolute;
  top: calc(100% + 2px);
  left: 0;
  z-index: 30;
  width: max-content;
  min-width: 100%;
  max-width: min(480px, calc(100vw - 32px));
  max-height: 280px;
  margin: 0;
  padding: 4px 0;
  overflow: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
  list-style: none;
}

.dropdown-list__option {
  display: flex;
  min-height: 32px;
  align-items: center;
  padding: 6px 12px;
  color: var(--color-text);
  cursor: pointer;
  line-height: var(--line-height-body);
  white-space: normal;
  overflow-wrap: anywhere;
}

.dropdown-list__option--active,
.dropdown-list__option:hover {
  background: var(--color-brand-soft);
}

.dropdown-list__option--selected {
  color: var(--color-brand);
  font-weight: 500;
}

.dropdown-list__option--disabled {
  color: var(--color-text-placeholder);
  cursor: not-allowed;
}

.dropdown-list__option--disabled:hover {
  background: transparent;
}
</style>
