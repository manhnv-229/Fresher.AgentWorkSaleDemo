<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { IconClockHour4 } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';
import { createTimeOptions, formatTime, parseTime, setDateTimeParts, today } from '../../utils/datePicker';
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
    placeholder: 'HH:mm',
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
const hourOptions = createTimeOptions(24);
const minuteOptions = createTimeOptions(60);
const pendingHours = ref('00');
const pendingMinutes = ref('00');
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
} = usePickerField(props, 'time-picker', isOpen);
const currentTimeLabel = computed(() => {
  const date = setDateTimeParts(today(), Number(pendingHours.value), Number(pendingMinutes.value), 0);
  return `${formatTime(date, false)}:00`;
});

watch(
  () => model.value,
  (value) => {
    inputValue.value = value;
    const parsedTime = parseTime(value, false);
    if (!parsedTime) {
      return;
    }

    pendingHours.value = String(parsedTime.hours).padStart(2, '0');
    pendingMinutes.value = String(parsedTime.minutes).padStart(2, '0');
  },
  { immediate: true }
);

function commitValue() {
  const date = setDateTimeParts(today(), Number(pendingHours.value), Number(pendingMinutes.value), 0);
  const nextValue = formatTime(date, false);
  model.value = nextValue;
  inputValue.value = nextValue;
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
  const parsedTime = parseTime(inputValue.value, false);
  if (inputValue.value.trim() === '') {
    model.value = '';
    inputValue.value = '';
    return;
  }

  if (!parsedTime) {
    inputValue.value = model.value;
    return;
  }

  pendingHours.value = String(parsedTime.hours).padStart(2, '0');
  pendingMinutes.value = String(parsedTime.minutes).padStart(2, '0');
  commitValue();
}

function selectHour(option: string) {
  pendingHours.value = option;
}

function selectMinute(option: string) {
  pendingMinutes.value = option;
}

function selectNow() {
  const nextDate = today();
  pendingHours.value = String(nextDate.getHours()).padStart(2, '0');
  pendingMinutes.value = String(nextDate.getMinutes()).padStart(2, '0');
}

function applySelection() {
  commitValue();
  closePopover();
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
  <div ref="rootRef" class="time-field">
    <component
      :is="hasLabel ? 'label' : 'div'"
      class="field picker-field time-field__picker"
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
            <IconClockHour4 :size="20" stroke-width="1.5" aria-hidden="true" />
          </button>
          <div v-if="showTooltip" class="field__tooltip" role="tooltip">
            {{ error }}
          </div>

        <div v-if="isOpen" class="picker__popover">
          <div class="picker__header">
            {{ currentTimeLabel }}
          </div>

          <div class="picker__divider"></div>

          <div class="picker__time-columns">
            <div class="picker__time-column">
              <button
                v-for="option in hourOptions"
                :key="`hour-${option}`"
                type="button"
                class="picker__time-item"
                :class="{ 'picker__time-item--selected': pendingHours === option }"
                @click="selectHour(option)"
              >
                {{ option }}
              </button>
            </div>

            <div class="picker__time-column">
              <button
                v-for="option in minuteOptions"
                :key="`minute-${option}`"
                type="button"
                class="picker__time-item"
                :class="{ 'picker__time-item--selected': pendingMinutes === option }"
                @click="selectMinute(option)"
              >
                {{ option }}
              </button>
            </div>
          </div>

          <div class="picker__divider"></div>

          <div class="picker__footer">
            <button type="button" class="picker__link" @click="selectNow">
              Bây giờ
            </button>

            <BaseButton type="button" @click="applySelection">
              Đồng ý
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
.time-field {
  position: relative;
  width: max-content;
  font-size: 13px;
  line-height: 1;
}

.time-field__picker :deep(.field__body) {
  gap: 0;
}

.time-field__picker :deep(.field__control) {
  width: 158px;
}

.time-field__picker :deep(.field input) {
  width: 158px;
  height: var(--field-height);
  padding: 0 36px 0 12px;
  border: 1px solid var(--color-brand);
  border-radius: var(--field-radius);
  background: var(--color-surface);
  color: var(--color-text-subtle);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  box-shadow: 0 0 0 3px rgba(53, 99, 255, 0.08);
}

.time-field__picker :deep(.field input::placeholder) {
  color: var(--color-text-subtle);
}

.time-field__picker :deep(.field input:hover),
.time-field__picker :deep(.field input:focus),
.time-field__picker :deep(.field--focus input) {
  border-color: var(--color-brand);
  box-shadow: 0 0 0 3px rgba(53, 99, 255, 0.08);
}

.time-field__picker :deep(.picker-field__action) {
  right: 10px;
  width: 17px;
  height: 17px;
  border-radius: 0;
  background: transparent;
  color: #4b5563;
}

.time-field__picker :deep(.picker-field__action:hover:not(:disabled)),
.time-field__picker :deep(.picker-field__action:focus-visible) {
  background: transparent;
  color: #4b5563;
}

.time-field__picker :deep(.picker-field__action svg) {
  width: 17px;
  height: 17px;
}

.picker__popover {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  z-index: 30;
  width: 228px;
  border: 1px solid var(--color-border);
  border-radius: 0 0 14px 14px;
  background: var(--color-surface);
  box-shadow: var(--shadow-surface);
  overflow: hidden;
}

.picker__header {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 48px;
  padding: 0 12px;
  color: #111827;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.picker__divider {
  height: 1px;
  background: var(--color-border);
}

.picker__time-columns {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  padding: 8px 12px 12px 0;
}

.picker__time-column {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  max-height: 248px;
  overflow: auto;
  padding: 0 6px;
  border-left: 1px solid var(--color-border);
}

.picker__time-item {
  width: 60px;
  height: var(--field-height);
  flex: 0 0 var(--field-height);
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: #202124;
  padding: 0 var(--button-padding-x);
  font-size: var(--font-size-body);
  font-weight: 500;
  line-height: var(--line-height-body);
  cursor: pointer;
}

.picker__time-item:hover:not(.picker__time-item--selected) {
  background: var(--color-surface-muted);
}

.picker__time-item--selected {
  background: var(--color-brand);
  color: #ffffff;
}

.picker__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 48px;
  padding: 0 12px 0 16px;
}

.picker__link {
  border: 0;
  padding: 0;
  background: transparent;
  color: var(--color-brand);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  cursor: pointer;
}

.picker__footer :deep(.button) {
  min-width: 88px;
  height: var(--field-height);
  border-radius: 8px;
  padding: 0 12px;
  font-size: var(--font-size-body);
}

.picker__time-column::-webkit-scrollbar {
  width: 6px;
}

.picker__time-column::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(107, 114, 128, 0.24);
}
</style>
