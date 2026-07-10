<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { IconCalendar } from '@tabler/icons-vue';
import CalendarPanel from './CalendarPanel.vue';
import { cloneDate, formatDate, parseDate, startOfMonth, today } from '../../utils/datePicker';
import { usePickerField } from './usePickerField';
import { useI18n } from '../../i18n';

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
    placeholder: 'DD/MM/YYYY',
    disabled: false,
    readonly: false,
    error: '',
    hint: '',
    ariaLabel: '',
    state: 'normal'
  }
);

const model = defineModel<string>({ default: '' });
// Cụm state này quản lý giá trị hiển thị, ngày đã chọn và ngày đang được duyệt trên lịch.
const rootRef = ref<HTMLElement | null>(null);
const { t } = useI18n();
const isOpen = ref(false);
const inputValue = ref(model.value);
const selectedDate = ref(parseDate(model.value));
const displayDate = ref(startOfMonth(selectedDate.value ?? today()));
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
} = usePickerField(props, 'date-picker', isOpen);

watch(
  () => model.value,
  (value) => {
    inputValue.value = value;
    selectedDate.value = parseDate(value);
    displayDate.value = startOfMonth(selectedDate.value ?? today());
  }
);

// Commit date vào model chỉ sau khi normalize sang chuỗi hiển thị chuẩn.
function commitDate(date: Date | null) {
  const nextValue = formatDate(date);
  model.value = nextValue;
  inputValue.value = nextValue;
  selectedDate.value = date ? cloneDate(date) : null;
}

// Popover chỉ mở khi field còn hoạt động.
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

// Input text được parse dần để giữ UX nhập tay và chọn từ calendar đồng nhất.
function handleInput(event: Event) {
  inputValue.value = (event.target as HTMLInputElement).value;
}

function handleBlur() {
  handleBlurState();
  const nextDate = parseDate(inputValue.value);
  if (inputValue.value.trim() === '') {
    commitDate(null);
    return;
  }

  if (nextDate) {
    commitDate(nextDate);
    displayDate.value = startOfMonth(nextDate);
  } else {
    inputValue.value = model.value;
  }
}

// Chọn ngày từ panel sẽ commit ngay về model và đóng popover.
function selectDate(date: Date) {
  commitDate(date);
  displayDate.value = startOfMonth(date);
  closePopover();
}

function selectToday() {
  const nextDate = today();
  commitDate(nextDate);
  displayDate.value = startOfMonth(nextDate);
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
            :placeholder="placeholder || t('placeholders.date')"
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
            :aria-label="t('actions.openPicker')"
            @mousedown.prevent
            @click="togglePopover"
          >
            <IconCalendar :size="20" stroke-width="1.5" aria-hidden="true" />
          </button>
          <div v-if="showTooltip" class="field__tooltip" role="tooltip">
            {{ error }}
          </div>

        <div v-if="isOpen" class="picker__popover">
          <CalendarPanel
            :display-date="displayDate"
            :selected-date="selectedDate"
            @navigate-month="displayDate = startOfMonth($event)"
            @navigate-year="displayDate = startOfMonth($event)"
            @select="selectDate"
            @action="selectToday"
          />
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
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-surface);
}
</style>
