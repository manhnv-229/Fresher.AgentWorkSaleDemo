<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { IconCalendarMonth, IconChevronLeft, IconChevronRight } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';
import { addYears, cloneDate, formatMonthYear, getMonthLabels, parseMonthYear, startOfMonth, today } from '../../utils/datePicker';
import { usePickerField } from './usePickerField';

const props = withDefaults(
  defineProps<{
    id?: string;
    name?: string;
    label?: string;
    labelPosition?: 'hidden' | 'top' | 'none';
    required?: boolean;
    placeholder?: string;
    disabled?: boolean;
    readonly?: boolean;
    error?: string;
    hint?: string;
    ariaLabel?: string;
    state?: 'normal' | 'hover' | 'focus' | 'disabled' | 'error' | 'readonly';
  }>(),
  {
    id: undefined,
    name: undefined,
    label: '',
    labelPosition: 'top',
    required: false,
    placeholder: 'MM/YYYY',
    disabled: false,
    readonly: false,
    error: '',
    hint: '',
    ariaLabel: '',
    state: 'normal'
  }
);

const model = defineModel<string>({ default: '' });
const rootRef = ref<HTMLElement | null>(null);
const isOpen = ref(false);
const inputValue = ref(model.value);
const selectedMonth = ref(parseMonthYear(model.value));
const displayDate = ref(startOfMonth(selectedMonth.value ?? today()));
const monthLabels = getMonthLabels();
const {
  inputId,
  inputAttrs,
  isDisabled,
  isHoverState,
  isInvalidState,
  isReadonlyState,
  isVisualFocused,
  hasLabel,
  showTooltip,
  describedBy,
  handleFocus,
  handleBlurState
} = usePickerField(props, 'month-year-picker', isOpen);

watch(
  () => model.value,
  (value) => {
    inputValue.value = value;
    selectedMonth.value = parseMonthYear(value);
    displayDate.value = startOfMonth(selectedMonth.value ?? today());
  }
);

function commitValue(date: Date | null) {
  const nextValue = formatMonthYear(date);
  model.value = nextValue;
  inputValue.value = nextValue;
  selectedMonth.value = date ? cloneDate(date) : null;
}

function openPopover() {
  if (isDisabled.value) {
    return;
  }

  isOpen.value = true;
}

function closePopover() {
  isOpen.value = false;
}

function togglePopover() {
  if (isOpen.value) {
    closePopover();
    return;
  }

  openPopover();
}

function handleInput(event: Event) {
  inputValue.value = (event.target as HTMLInputElement).value;
}

function handleBlur() {
  handleBlurState();
  const nextMonth = parseMonthYear(inputValue.value);
  if (inputValue.value.trim() === '') {
    commitValue(null);
    return;
  }

  if (!nextMonth) {
    inputValue.value = model.value;
    return;
  }

  commitValue(nextMonth);
  displayDate.value = startOfMonth(nextMonth);
}

function selectMonth(monthIndex: number) {
  const nextDate = new Date(displayDate.value.getFullYear(), monthIndex, 1);
  commitValue(nextDate);
  displayDate.value = nextDate;
  closePopover();
}

function selectCurrentMonth() {
  const nextDate = startOfMonth(today());
  commitValue(nextDate);
  displayDate.value = nextDate;
  closePopover();
}

function isSelected(monthIndex: number) {
  return Boolean(
    selectedMonth.value
    && selectedMonth.value.getFullYear() === displayDate.value.getFullYear()
    && selectedMonth.value.getMonth() === monthIndex
  );
}

function handleDocumentPointerDown(event: PointerEvent) {
  const target = event.target;
  if (target instanceof Node && rootRef.value?.contains(target)) {
    return;
  }

  closePopover();
}

onMounted(() => {
  document.addEventListener('pointerdown', handleDocumentPointerDown);
});

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', handleDocumentPointerDown);
});
</script>

<template>
  <div ref="rootRef" class="picker">
    <component
      :is="hasLabel ? 'label' : 'div'"
      class="field picker-field"
      :class="[
        `field--label-${labelPosition}`,
        {
          'field--invalid': isInvalidState,
          'field--hover': isHoverState,
          'field--focus': isVisualFocused,
          'field--readonly': isReadonlyState,
          'field--disabled': isDisabled
        }
      ]"
    >
      <span v-if="hasLabel && labelPosition === 'hidden'" class="sr-only">{{ label }}</span>
      <span v-else-if="hasLabel" class="field__label">
        {{ label }}
        <span v-if="required" class="field__required" aria-hidden="true">*</span>
      </span>

      <div class="field__body">
        <div class="field__control">
          <input
            :id="inputId"
            class="picker-field__input"
            :name="name"
            :value="inputValue"
            :placeholder="placeholder"
            :disabled="isDisabled"
            :readonly="isReadonlyState"
            :aria-label="!label || labelPosition === 'none' ? (ariaLabel || label || placeholder || undefined) : undefined"
            :aria-invalid="isInvalidState ? 'true' : 'false'"
            :aria-describedby="isInvalidState ? describedBy : hint ? `${inputId}-hint` : undefined"
            v-bind="inputAttrs"
            @input="handleInput"
            @focus="handleFocus"
            @blur="handleBlur"
          />
          <button
            type="button"
            class="field__action picker-field__action"
            :disabled="isDisabled"
            aria-label="Mở bộ chọn"
            @mousedown.prevent
            @click="togglePopover"
          >
            <IconCalendarMonth :size="16" stroke-width="1.5" aria-hidden="true" />
          </button>
          <div v-if="showTooltip" class="field__tooltip" role="tooltip">
            {{ error }}
          </div>

        <div v-if="isOpen" class="picker__popover">
          <div class="picker__header">
            <button type="button" class="picker__nav" aria-label="Năm trước" @click="displayDate = addYears(displayDate, -1)">
              <IconChevronLeft :size="16" stroke-width="1.5" aria-hidden="true" />
            </button>
            <span class="picker__year">{{ displayDate.getFullYear() }}</span>
            <button type="button" class="picker__nav" aria-label="Năm sau" @click="displayDate = addYears(displayDate, 1)">
              <IconChevronRight :size="16" stroke-width="1.5" aria-hidden="true" />
            </button>
          </div>

          <div class="picker__divider"></div>

          <div class="picker__month-grid">
            <BaseButton
              v-for="(labelValue, monthIndex) in monthLabels"
              :key="labelValue"
              variant="neutralInverse"
              class="picker__month"
              :class="{ 'picker__month--selected': isSelected(monthIndex) }"
              @click="selectMonth(monthIndex)"
            >
              {{ labelValue }}
            </BaseButton>
          </div>

          <div class="picker__divider"></div>

          <div class="picker__footer">
            <BaseButton type="button" variant="neutralInverse" class="picker__action" @click="selectCurrentMonth">
              Tháng này
            </BaseButton>
          </div>
        </div>
        </div>

        <span
          v-if="isInvalidState && !showTooltip"
          :id="`${inputId}-error`"
          class="field__feedback field__feedback--error"
          role="alert"
        >
          {{ error }}
        </span>
        <span v-else-if="hint" :id="`${inputId}-hint`" class="field__feedback">
          {{ hint }}
        </span>
      </div>
    </component>
  </div>
</template>

<style scoped>
.picker {
  position: relative;
}

.picker__popover {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  z-index: 30;
  min-width: 280px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-surface);
}

.picker__header,
.picker__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 48px;
  padding: 0 16px;
}

.picker__nav {
  display: inline-grid;
  width: 32px;
  height: 32px;
  place-items: center;
  padding: 0;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: var(--color-text-subtle);
}

.picker__nav:hover {
  background: var(--color-brand-soft);
  color: var(--color-text);
}

.picker__year {
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

.picker__divider {
  height: 1px;
  background: var(--color-border);
}

.picker__month-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  padding: 12px;
}

:deep(.button.picker__month) {
  width: 100%;
  height: 32px;
  border-color: transparent;
  border-radius: 12px;
  background: transparent;
  color: var(--color-text);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

:deep(.button.picker__month:hover:not(:disabled)) {
  border-color: transparent;
  background: var(--color-surface-muted);
}

:deep(.button.picker__month.picker__month--selected),
:deep(.button.picker__month.picker__month--selected:hover:not(:disabled)) {
  border-color: var(--color-brand);
  background: var(--color-brand);
  color: #ffffff;
}

:deep(.button.picker__action) {
  width: 100%;
  min-height: 32px;
  border-color: transparent;
  background: transparent;
  color: var(--color-brand);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

:deep(.button.picker__action:hover:not(:disabled)) {
  border-color: transparent;
  background: var(--color-surface-muted);
}
</style>
