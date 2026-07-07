<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, useAttrs, watch } from 'vue';
import { IconCheck, IconChevronDown, IconLoader2, IconSearch, IconX } from '@tabler/icons-vue';

export type ComboboxOption = {
  label: string;
  value: string;
  description?: string;
  image?: string;
  disabled?: boolean;
};

defineOptions({
  inheritAttrs: false
});

const props = withDefaults(
  defineProps<{
    id?: string;
    name?: string;
    options: ComboboxOption[];
    placeholder?: string;
    disabled?: boolean;
    loading?: boolean;
    multiple?: boolean;
    split?: boolean;
    label?: string;
    labelPosition?: 'hidden' | 'top' | 'none';
    required?: boolean;
    error?: string;
    hint?: string;
    ariaLabel?: string;
    state?: 'normal' | 'hover' | 'focus' | 'disabled' | 'error' | 'loading';
    noResultsText?: string;
    loadingText?: string;
  }>(),
  {
    placeholder: 'Chọn giá trị',
    disabled: false,
    loading: false,
    multiple: false,
    split: false,
    labelPosition: 'top',
    required: false,
    error: '',
    hint: '',
    ariaLabel: '',
    state: 'normal',
    noResultsText: 'Không có kết quả',
    loadingText: 'Đang tải...'
  }
);

const emit = defineEmits<{
  change: [value: string | string[], option?: ComboboxOption];
  open: [];
  close: [];
  advancedClick: [];
  search: [query: string];
  focus: [event: FocusEvent];
  blur: [event: FocusEvent];
}>();

const model = defineModel<string | string[]>({ required: true });
const attrs = useAttrs();
const rootRef = ref<HTMLElement | null>(null);
const inputRef = ref<HTMLInputElement | null>(null);
const isOpen = ref(false);
const isFocused = ref(false);
const isHiddenTagsOpen = ref(false);
const query = ref('');
const activeIndex = ref(-1);
const selectionOrder = ref<string[]>([]);

const inputId = computed(() => props.id || (attrs.id as string | undefined) || props.name || 'combobox');
const listboxId = computed(() => `${inputId.value}-listbox`);
const errorId = computed(() => `${inputId.value}-error`);
const hintId = computed(() => `${inputId.value}-hint`);
const hasLabel = computed(() => props.labelPosition !== 'none' && Boolean(props.label));
const wrapperClass = computed(() => attrs.class);
const isDisabled = computed(() => props.disabled || props.state === 'disabled');
const isLoading = computed(() => props.loading || props.state === 'loading');
const isHoverState = computed(() => props.state === 'hover');
const isFocusState = computed(() => props.state === 'focus');
const isInvalidState = computed(() => Boolean(props.error) || props.state === 'error');
const isVisualFocused = computed(() => isFocused.value || isFocusState.value || isOpen.value);
const modelValues = computed(() => (Array.isArray(model.value) ? model.value : model.value ? [model.value] : []));
const selectedOptions = computed(() => props.options.filter((option) => modelValues.value.includes(option.value)));
const selectedOrderedOptions = computed(() =>
  selectionOrder.value
    .map((value) => props.options.find((option) => option.value === value))
    .filter((option): option is ComboboxOption => Boolean(option))
);
const hiddenTags = computed(() => (props.multiple ? selectedOrderedOptions.value.slice(0, -2) : []));
const expandedTags = computed(() => (props.multiple ? selectedOrderedOptions.value : []));
const visibleTags = computed(() => (props.multiple ? selectedOrderedOptions.value.slice(-2) : []));
const hiddenTagCount = computed(() => hiddenTags.value.length);
const selectedSingleOption = computed(() => (!props.multiple ? selectedOptions.value[0] : undefined));
const inputDisplayValue = computed(() => {
  if (props.multiple || isOpen.value || query.value) {
    return query.value;
  }

  return selectedSingleOption.value?.label || '';
});
const filteredOptions = computed(() => {
  const normalizedQuery = query.value.trim().toLowerCase();

  if (!normalizedQuery) {
    return props.options;
  }

  return props.options.filter((option) => {
    const label = option.label.toLowerCase();
    const description = option.description?.toLowerCase() || '';
    return label.includes(normalizedQuery) || description.includes(normalizedQuery);
  });
});
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
    'field--disabled': isDisabled.value,
    'field--loading': isLoading.value,
    'combobox--multiple': props.multiple,
    'combobox--split': props.split
  }
]);

function isSelected(value: string) {
  return modelValues.value.includes(value);
}

function firstEnabledIndex(startIndex = 0) {
  return filteredOptions.value.findIndex((option, index) => index >= startIndex && !option.disabled);
}

function nextEnabledIndex(direction: 1 | -1) {
  if (!filteredOptions.value.length) {
    return -1;
  }

  let index = activeIndex.value;
  for (let offset = 0; offset < filteredOptions.value.length; offset += 1) {
    index = (index + direction + filteredOptions.value.length) % filteredOptions.value.length;
    if (!filteredOptions.value[index]?.disabled) {
      return index;
    }
  }

  return -1;
}

function lastEnabledIndex() {
  for (let index = filteredOptions.value.length - 1; index >= 0; index -= 1) {
    if (!filteredOptions.value[index]?.disabled) {
      return index;
    }
  }

  return -1;
}

function setActiveToSelected() {
  const selectedIndex = filteredOptions.value.findIndex((option) => isSelected(option.value) && !option.disabled);
  activeIndex.value = selectedIndex >= 0 ? selectedIndex : firstEnabledIndex();
}

function openCombobox() {
  if (isDisabled.value || isOpen.value) {
    return;
  }

  isHiddenTagsOpen.value = false;
  isOpen.value = true;
  setActiveToSelected();
  emit('open');
}

function closeCombobox() {
  if (!isOpen.value) {
    return;
  }

  isOpen.value = false;
  activeIndex.value = -1;
  query.value = '';
  emit('close');
}

function focusInput() {
  nextTick(() => inputRef.value?.focus());
}

function updateModel(option: ComboboxOption) {
  if (!option || option.disabled) {
    return;
  }

  if (props.multiple) {
    const nextOrder = isSelected(option.value)
      ? selectionOrder.value.filter((value) => value !== option.value)
      : [...selectionOrder.value.filter((value) => value !== option.value), option.value];

    selectionOrder.value = nextOrder;
    model.value = nextOrder;
    emit('change', nextOrder, option);
    query.value = '';
    isHiddenTagsOpen.value = false;
    setActiveToSelected();
    focusInput();
    return;
  }

  model.value = option.value;
  emit('change', option.value, option);
  closeCombobox();
  focusInput();
}

function removeValue(value: string) {
  if (isDisabled.value || !props.multiple) {
    return;
  }

  const nextValue = modelValues.value.filter((item) => item !== value);
  const nextOrder = selectionOrder.value.filter((item) => item !== value);
  selectionOrder.value = nextOrder;
  model.value = nextValue;
  emit('change', nextValue);
  isHiddenTagsOpen.value = false;
  focusInput();
}

function toggleHiddenTags() {
  if (isDisabled.value || !hiddenTagCount.value) {
    return;
  }

  const shouldOpen = !isHiddenTagsOpen.value;
  closeCombobox();
  isHiddenTagsOpen.value = shouldOpen;
}

function handleInput(event: Event) {
  const target = event.target as HTMLInputElement;
  query.value = target.value;
  emit('search', query.value);
  openCombobox();
  activeIndex.value = firstEnabledIndex();
}

function handleFocus(event: FocusEvent) {
  isFocused.value = true;
  openCombobox();
  emit('focus', event);
}

function handleBlur(event: FocusEvent) {
  isFocused.value = false;
  emit('blur', event);
}

function handleKeydown(event: KeyboardEvent) {
  if (isDisabled.value) {
    return;
  }

  if (event.key === 'ArrowDown') {
    event.preventDefault();
    openCombobox();
    activeIndex.value = nextEnabledIndex(1);
  } else if (event.key === 'ArrowUp') {
    event.preventDefault();
    openCombobox();
    activeIndex.value = nextEnabledIndex(-1);
  } else if (event.key === 'Home' && isOpen.value) {
    event.preventDefault();
    activeIndex.value = firstEnabledIndex();
  } else if (event.key === 'End' && isOpen.value) {
    event.preventDefault();
    activeIndex.value = lastEnabledIndex();
  } else if (event.key === 'Enter' && isOpen.value) {
    event.preventDefault();
    updateModel(filteredOptions.value[activeIndex.value]);
  } else if (event.key === 'Escape') {
    event.preventDefault();
    closeCombobox();
  } else if (event.key === 'Backspace' && props.multiple && !query.value && modelValues.value.length) {
    removeValue(selectionOrder.value[selectionOrder.value.length - 1]);
  }
}

function handleToggleClick() {
  if (isOpen.value) {
    closeCombobox();
    focusInput();
    return;
  }

  openCombobox();
  focusInput();
}

function handleAdvancedClick() {
  if (isDisabled.value) {
    return;
  }

  emit('advancedClick');
}

function handleDocumentPointerDown(event: PointerEvent) {
  const target = event.target;

  if (target instanceof Node && rootRef.value?.contains(target)) {
    return;
  }

  closeCombobox();
  isHiddenTagsOpen.value = false;
}

watch(
  modelValues,
  (values) => {
    selectionOrder.value = [
      ...selectionOrder.value.filter((value) => values.includes(value)),
      ...values.filter((value) => !selectionOrder.value.includes(value))
    ];

    if (selectionOrder.value.length <= 2) {
      isHiddenTagsOpen.value = false;
    }
  },
  { immediate: true }
);

watch(filteredOptions, () => {
  activeIndex.value = firstEnabledIndex();
});

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
    class="field combobox"
    :class="fieldClasses"
  >
    <span v-if="hasLabel && labelPosition === 'hidden'" class="sr-only">{{ label }}</span>
    <span v-else-if="hasLabel" class="field__label">
      {{ label }}
      <span v-if="required" class="field__required" aria-hidden="true">*</span>
    </span>

    <div class="field__body">
      <div class="field__control combobox__anchor">
        <div class="combobox__control" @click="focusInput">
          <div v-if="multiple && selectedOptions.length" class="combobox__tags">
            <button
              v-if="hiddenTagCount"
              type="button"
              class="combobox__tag combobox__tag--more"
              :aria-label="`Mở rộng ${selectedOrderedOptions.length} giá trị đã chọn`"
              :aria-expanded="isHiddenTagsOpen ? 'true' : 'false'"
              :disabled="isDisabled"
              @mousedown.stop.prevent="toggleHiddenTags"
              @keydown.enter.stop.prevent="toggleHiddenTags"
              @keydown.space.stop.prevent="toggleHiddenTags"
            >
              {{ hiddenTagCount }}
            </button>
            <span v-for="option in visibleTags" :key="option.value" class="combobox__tag">
              <span class="combobox__tag-text">{{ option.label }}</span>
              <button
                type="button"
                class="combobox__tag-remove"
                :aria-label="`Xóa ${option.label}`"
                :disabled="isDisabled"
                @mousedown.stop.prevent="removeValue(option.value)"
                @keydown.enter.stop.prevent="removeValue(option.value)"
                @keydown.space.stop.prevent="removeValue(option.value)"
              >
                <IconX :size="12" stroke-width="1.8" aria-hidden="true" />
              </button>
            </span>
          </div>

          <input
            :id="inputId"
            ref="inputRef"
            class="combobox__input"
            :class="{ 'combobox__input--placeholder': !selectedSingleOption && !query }"
            :name="name"
            :value="inputDisplayValue"
            :placeholder="multiple && selectedOptions.length ? '' : placeholder"
            :disabled="isDisabled"
            :readonly="!multiple && Boolean(selectedSingleOption) && !isOpen"
            :aria-label="!label || labelPosition === 'none' ? (ariaLabel || label || placeholder || undefined) : undefined"
            :aria-invalid="isInvalidState ? 'true' : 'false'"
            :aria-describedby="describedBy"
            :aria-controls="listboxId"
            :aria-expanded="isOpen ? 'true' : 'false'"
            :aria-activedescendant="isOpen ? activeOptionId : undefined"
            aria-autocomplete="list"
            aria-haspopup="listbox"
            role="combobox"
            v-bind="{ ...attrs, class: undefined }"
            @input="handleInput"
            @focus="handleFocus"
            @blur="handleBlur"
            @keydown="handleKeydown"
          />

          <button
            v-if="split"
            type="button"
            class="combobox__icon-button"
            :disabled="isDisabled"
            aria-label="Mở tìm kiếm nâng cao"
            @mousedown.stop.prevent="handleAdvancedClick"
            @keydown.enter.stop.prevent="handleAdvancedClick"
            @keydown.space.stop.prevent="handleAdvancedClick"
          >
            <IconSearch :size="16" stroke-width="1.5" aria-hidden="true" />
          </button>

          <button
            type="button"
            class="combobox__icon-button"
            :disabled="isDisabled"
            aria-label="Mở danh sách"
            @mousedown.stop.prevent="handleToggleClick"
            @keydown.enter.stop.prevent="handleToggleClick"
            @keydown.space.stop.prevent="handleToggleClick"
          >
            <IconLoader2 v-if="isLoading" :size="16" stroke="1.5" class="spin" aria-hidden="true" />
            <IconChevronDown
              v-else
              class="combobox__chevron"
              :class="{ 'combobox__chevron--open': isOpen }"
              :size="16"
              stroke-width="1.5"
              aria-hidden="true"
            />
          </button>
        </div>

        <div v-if="multiple && isHiddenTagsOpen" class="combobox__hidden-tags" role="dialog">
          <button
            v-for="option in expandedTags"
            :key="option.value"
            type="button"
            class="combobox__hidden-tag"
            :aria-label="`Xóa ${option.label}`"
            @mousedown.stop.prevent="removeValue(option.value)"
            @keydown.enter.stop.prevent="removeValue(option.value)"
            @keydown.space.stop.prevent="removeValue(option.value)"
          >
            <span>{{ option.label }}</span>
            <IconX :size="12" stroke-width="1.8" aria-hidden="true" />
          </button>
        </div>

        <ul
          v-if="isOpen"
          :id="listboxId"
          class="combobox__menu"
          role="listbox"
          :aria-labelledby="inputId"
          :aria-multiselectable="multiple ? 'true' : undefined"
        >
          <li v-if="isLoading" class="combobox__empty">{{ loadingText }}</li>
          <li v-else-if="!filteredOptions.length" class="combobox__empty">{{ noResultsText }}</li>
          <li
            v-for="(option, index) in filteredOptions"
            v-else
            :id="`${inputId}-option-${index}`"
            :key="option.value"
            class="combobox__option"
            :class="{
              'combobox__option--active': index === activeIndex,
              'combobox__option--selected': isSelected(option.value),
              'combobox__option--disabled': option.disabled
            }"
            role="option"
            :aria-selected="isSelected(option.value) ? 'true' : 'false'"
            :aria-disabled="option.disabled ? 'true' : undefined"
            @mouseenter="activeIndex = option.disabled ? activeIndex : index"
            @mousedown.stop.prevent="updateModel(option)"
          >
            <img v-if="option.image" class="combobox__option-image" :src="option.image" alt="" />
            <span class="combobox__option-body">
              <span class="combobox__option-label">{{ option.label }}</span>
              <span v-if="option.description" class="combobox__option-description">{{ option.description }}</span>
            </span>
            <IconCheck
              v-if="isSelected(option.value)"
              class="combobox__option-check"
              :size="16"
              stroke="1.5"
              aria-hidden="true"
            />
          </li>
        </ul>
      </div>

      <span
        v-if="isInvalidState"
        :id="errorId"
        class="field__feedback field__feedback--error combobox__error"
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
.combobox {
  position: relative;
}

.combobox__anchor {
  position: relative;
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  transition: box-shadow 120ms ease;
}

.combobox__anchor:hover,
.field--hover .combobox__anchor,
.field--focus .combobox__anchor,
.combobox__anchor:focus-within {
  border-color: var(--color-brand);
  box-shadow: 0 0 0 3px rgba(53, 99, 255, 0.08);
}

.field--invalid .combobox__anchor {
  border-color: var(--color-danger);
  box-shadow: 0 0 0 3px rgba(220, 38, 38, 0.08);
}

.field--disabled .combobox__anchor {
  background: var(--color-surface-muted);
}

.combobox__control {
  display: flex;
  width: 100%;
  height: var(--field-height);
  min-width: 0;
  align-items: center;
  gap: 4px;
  overflow: hidden;
  padding: 3px 8px 3px var(--field-padding-x);
  border: 0;
  border-radius: inherit;
  background: transparent;
  box-shadow: none;
  color: var(--color-text);
  transition:
    border-color 120ms ease,
    background 120ms ease;
}


.combobox__input {
  flex: 1 1 auto;
  width: auto;
  min-width: 24px;
  height: 24px;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-text);
  font: inherit;
  line-height: var(--line-height-body);
  outline: none;
  box-shadow: none;
}

.combobox__input::placeholder {
  color: var(--color-text-placeholder);
}

.field .combobox__input:hover,
.field--hover .combobox__input,
.field--focus .combobox__input,
.field--invalid .combobox__input,
.field--validate .combobox__input,
.field--verifying .combobox__input,
.combobox__input:hover {
  border-color: transparent;
  box-shadow: none;
  background: transparent;
}

.combobox__input:focus,
.combobox__input:focus-visible {
  outline: none;
  box-shadow: none;
  background: transparent;
}

.combobox__input:read-only {
  cursor: pointer;
}

.combobox__input:disabled {
  color: var(--color-text-subtle);
}

.combobox__tags {
  display: flex;
  flex: 0 1 auto;
  min-width: 0;
  width: auto;
  height: 24px;
  align-items: center;
  gap: 4px;
  overflow: hidden;
}

.combobox__tag {
  display: inline-flex;
  flex: 0 1 auto;
  min-width: 0;
  max-width: 120px;
  height: 24px;
  align-items: center;
  gap: 4px;
  padding: 0 6px;
  border-radius: 6px;
  border: 0;
  background: var(--color-border);
  color: var(--color-text);
  font-size: 12px;
  font-weight: 500;
  line-height: 16px;
  white-space: nowrap;
}

.combobox__tag-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.combobox__tag-remove {
  display: inline-grid;
  flex: 0 0 14px;
  width: 14px;
  height: 14px;
  place-items: center;
  padding: 0;
  border: 0;
  border-radius: 4px;
  background: transparent;
  color: currentColor;
}

.combobox__tag-remove:hover:not(:disabled) {
  background: rgba(17, 24, 39, 0.08);
}

.combobox__tag-remove:focus,
.combobox__tag-remove:focus-visible {
  outline: none;
  box-shadow: none;
}

.combobox__tag--more {
  flex: 0 0 28px;
  width: 28px;
  justify-content: center;
  max-width: none;
  padding: 0 6px;
}

.combobox__tag--more:hover:not(:disabled),
.combobox__tag--more:focus-visible {
  background: var(--color-brand-soft);
  color: var(--color-brand);
  outline: none;
  box-shadow: none;
}

.combobox__icon-button {
  display: inline-grid;
  flex: 0 0 16px;
  width: 16px;
  height: 16px;
  place-items: center;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-text-subtle);
  line-height: 1;
}

.combobox__icon-button:focus,
.combobox__icon-button:focus-visible {
  outline: none;
  box-shadow: none;
}

.combobox__icon-button:disabled {
  color: var(--color-text-placeholder);
}

.combobox__hidden-tags {
  position: absolute;
  top: 100%;
  left: 0;
  z-index: 35;
  display: flex;
  max-width: min(360px, calc(100vw - 32px));
  flex-wrap: wrap;
  gap: 4px;
  padding: 8px;
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
}

.combobox__hidden-tag {
  display: inline-flex;
  max-width: 160px;
  height: 24px;
  align-items: center;
  gap: 4px;
  padding: 0 6px;
  border: 0;
  border-radius: 6px;
  background: var(--color-border);
  color: var(--color-text);
  font: inherit;
  font-size: 12px;
  font-weight: 500;
}

.combobox__hidden-tag span {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.combobox__hidden-tag:hover {
  background: var(--color-brand-soft);
  color: var(--color-brand);
}

.combobox__chevron {
  transition: transform 120ms ease;
}

.combobox__chevron--open {
  transform: rotate(180deg);
}

.combobox__menu {
  position: absolute;
  top: 100%;
  left: 0;
  z-index: 30;
  width: max-content;
  min-width: 100%;
  max-width: min(520px, calc(100vw - 32px));
  max-height: 336px;
  margin: 0;
  padding: 8px 0;
  overflow: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  box-shadow: var(--shadow-card);
  list-style: none;
}

.combobox__option {
  display: flex;
  min-height: 32px;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  color: var(--color-text);
  cursor: pointer;
  line-height: var(--line-height-body);
  white-space: normal;
  overflow-wrap: anywhere;
}

.combobox__option--active,
.combobox__option:hover {
  background: var(--color-brand-soft);
}

.combobox__option--selected .combobox__option-label {
  color: var(--color-brand);
  font-weight: 500;
}

.combobox__option--disabled {
  color: var(--color-text-placeholder);
  cursor: not-allowed;
}

.combobox__option--disabled:hover {
  background: transparent;
}

.combobox__option-image {
  flex: 0 0 24px;
  width: 24px;
  height: 24px;
  border-radius: 6px;
  object-fit: cover;
}

.combobox__option-body {
  display: grid;
  flex: 1 1 auto;
  min-width: 0;
  gap: 2px;
}

.combobox__option-description {
  color: var(--color-text-subtle);
  font-size: 12px;
  line-height: 16px;
}

.combobox__option-check {
  flex: 0 0 16px;
  color: var(--color-brand);
}

.combobox__empty {
  display: flex;
  min-height: 32px;
  align-items: center;
  padding: 0 12px;
  color: var(--color-text-subtle);
}

.combobox__error {
  position: relative;
}
</style>
