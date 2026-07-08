<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { IconCalendar } from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';
import CalendarPanel from './CalendarPanel.vue';
import { usePickerField } from './usePickerField';
import {
  cloneDate,
  DateRangeModel,
  DateRangePreset,
  formatRangeLabel,
  normalizeDateRange,
  parseDate,
  startOfMonth,
  today,
  toDateRangeModel
} from '../../utils/datePicker';

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
    presets?: DateRangePreset[];
  }>(),
  {
    id: undefined,
    name: undefined,
    label: '',
    labelPosition: 'top',
    required: false,
    placeholder: 'DD/MM/YYYY - DD/MM/YYYY',
    disabled: false,
    readonly: false,
    error: '',
    hint: '',
    ariaLabel: '',
    state: 'normal',
    presets: () => []
  }
);

const model = defineModel<DateRangeModel>({
  default: () => ({
    start: '',
    end: ''
  })
});

const rootRef = ref<HTMLElement | null>(null);
const isOpen = ref(false);
const inputValue = ref(formatRangeLabel(parseDate(model.value.start), parseDate(model.value.end)));
const startDate = ref<Date | null>(parseDate(model.value.start));
const endDate = ref<Date | null>(parseDate(model.value.end));
const pendingStartDate = ref<Date | null>(startDate.value ? cloneDate(startDate.value) : null);
const pendingEndDate = ref<Date | null>(endDate.value ? cloneDate(endDate.value) : null);
const displayDate = ref(startOfMonth(startDate.value ?? today()));
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
} = usePickerField(props, 'date-range-picker-advanced', isOpen);

watch(
  () => model.value,
  (value) => {
    startDate.value = parseDate(value.start);
    endDate.value = parseDate(value.end);
    pendingStartDate.value = startDate.value ? cloneDate(startDate.value) : null;
    pendingEndDate.value = endDate.value ? cloneDate(endDate.value) : null;
    displayDate.value = startOfMonth(startDate.value ?? today());
    inputValue.value = formatRangeLabel(startDate.value, endDate.value);
  },
  { deep: true, immediate: true }
);

function commitSelection() {
  const normalized = normalizeDateRange(pendingStartDate.value, pendingEndDate.value);
  startDate.value = normalized.startDate;
  endDate.value = normalized.endDate;
  model.value = toDateRangeModel(startDate.value, endDate.value);
  inputValue.value = formatRangeLabel(startDate.value, endDate.value);
}

function openPopover() {
  if (isDisabled.value) {
    return;
  }

  pendingStartDate.value = startDate.value ? cloneDate(startDate.value) : null;
  pendingEndDate.value = endDate.value ? cloneDate(endDate.value) : null;
  displayDate.value = startOfMonth(pendingStartDate.value ?? today());
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
  inputValue.value = formatRangeLabel(startDate.value, endDate.value);
}

function selectDate(date: Date) {
  if (!pendingStartDate.value || (pendingStartDate.value && pendingEndDate.value)) {
    pendingStartDate.value = cloneDate(date);
    pendingEndDate.value = null;
    return;
  }

  if (date.getTime() < pendingStartDate.value.getTime()) {
    pendingStartDate.value = cloneDate(date);
    pendingEndDate.value = null;
    return;
  }

  pendingEndDate.value = cloneDate(date);
}

function applyPreset(preset: DateRangePreset) {
  pendingStartDate.value = parseDate(preset.value.start);
  pendingEndDate.value = parseDate(preset.value.end);
  displayDate.value = startOfMonth(pendingStartDate.value ?? today());
  applySelection();
}

function applySelection() {
  commitSelection();
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
            <IconCalendar :size="16" stroke-width="1.5" aria-hidden="true" />
          </button>
          <div v-if="showTooltip" class="field__tooltip" role="tooltip">
            {{ error }}
          </div>

        <div v-if="isOpen" class="picker__popover" :class="{ 'picker__popover--presets': presets.length > 0 }">
          <aside v-if="presets.length > 0" class="picker__presets">
            <div class="picker__section-header">Khoảng cố định</div>
            <button
              v-for="preset in presets"
              :key="preset.label"
              type="button"
              class="picker__preset"
              @click="applyPreset(preset)"
            >
              {{ preset.label }}
            </button>
          </aside>

          <div class="picker__content">
            <CalendarPanel
              :display-date="displayDate"
              :range-start="pendingStartDate"
              :range-end="pendingEndDate"
              :action-label="''"
              @navigate-month="displayDate = startOfMonth($event)"
              @navigate-year="displayDate = startOfMonth($event)"
              @select="selectDate"
            />

            <div class="picker__actions">
              <BaseButton type="button" variant="secondary" @click="closePopover">Hủy</BaseButton>
              <BaseButton type="button" @click="applySelection">Đồng ý</BaseButton>
            </div>
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
  min-width: 296px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-surface);
}

.picker__popover--presets {
  display: grid;
  grid-template-columns: 180px minmax(0, 1fr);
}

.picker__presets {
  display: grid;
  align-content: start;
  gap: 8px;
  padding: 12px;
  border-right: 1px solid var(--color-border);
}

.picker__preset {
  min-height: 32px;
  padding: 0 10px;
  border: 0;
  border-radius: 8px;
  background: var(--color-surface);
  color: var(--color-text);
  text-align: left;
}

.picker__preset:hover {
  border-color: var(--color-brand);
  background: var(--color-brand-soft);
}

.picker__content {
  display: grid;
}

.picker__section-header {
  font-size: 12px;
  line-height: 16px;
  font-weight: 500;
  color: var(--color-text-subtle);
}

.picker__actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding: 12px;
}
</style>
