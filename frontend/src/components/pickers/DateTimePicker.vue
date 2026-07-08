<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import {
  IconCalendarTime,
  IconChevronsLeft,
  IconChevronLeft,
  IconChevronRight,
  IconChevronsRight
} from '@tabler/icons-vue';
import BaseButton from '../buttons/BaseButton.vue';
import {
  addMonths,
  addYears,
  cloneDate,
  createTimeOptions,
  formatDate,
  formatDateTime,
  formatTime,
  getCalendarMatrix,
  getWeekdayLabels,
  isSameDay,
  isSameMonth,
  parseDateTime,
  setDateTimeParts,
  startOfMonth,
  today
} from '../../utils/datePicker';
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
    showSeconds?: boolean;
  }>(),
  {
    id: undefined,
    name: undefined,
    label: '',
    labelPosition: 'top',
    required: false,
    placeholder: 'DD/MM/YYYY HH:MM:SS',
    disabled: false,
    readonly: false,
    error: '',
    hint: '',
    ariaLabel: '',
    state: 'normal',
    showSeconds: true
  }
);

const model = defineModel<string>({ default: '' });
const rootRef = ref<HTMLElement | null>(null);
const isOpen = ref(false);
const inputValue = ref(model.value);
const selectedDateTime = ref(parseDateTime(model.value, props.showSeconds));
const pendingDateTime = ref(selectedDateTime.value ? cloneDate(selectedDateTime.value) : today());
const displayDate = ref(startOfMonth(selectedDateTime.value ?? today()));
const hourOptions = createTimeOptions(24);
const minuteOptions = createTimeOptions(60);
const secondOptions = createTimeOptions(60);
const weekdayLabels = getWeekdayLabels();

const pendingHours = ref(String(pendingDateTime.value.getHours()).padStart(2, '0'));
const pendingMinutes = ref(String(pendingDateTime.value.getMinutes()).padStart(2, '0'));
const pendingSeconds = ref(String(pendingDateTime.value.getSeconds()).padStart(2, '0'));
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
} = usePickerField(props, 'date-time-picker', isOpen);
const calendar = computed(() => getCalendarMatrix(displayDate.value));
const currentTimeLabel = computed(() => formatTime(pendingDateTime.value, props.showSeconds));

watch(
  () => model.value,
  (value) => {
    inputValue.value = value;
    selectedDateTime.value = parseDateTime(value, props.showSeconds);
    pendingDateTime.value = selectedDateTime.value ? cloneDate(selectedDateTime.value) : today();
    displayDate.value = startOfMonth(selectedDateTime.value ?? today());
    syncTimeParts();
  }
);

function syncTimeParts() {
  pendingHours.value = String(pendingDateTime.value.getHours()).padStart(2, '0');
  pendingMinutes.value = String(pendingDateTime.value.getMinutes()).padStart(2, '0');
  pendingSeconds.value = String(pendingDateTime.value.getSeconds()).padStart(2, '0');
}

function commitValue(date: Date | null) {
  const nextValue = formatDateTime(date, props.showSeconds);
  model.value = nextValue;
  inputValue.value = nextValue;
  selectedDateTime.value = date ? cloneDate(date) : null;
}

function openPopover() {
  if (isDisabled.value) {
    return;
  }

  pendingDateTime.value = selectedDateTime.value ? cloneDate(selectedDateTime.value) : today();
  displayDate.value = startOfMonth(pendingDateTime.value);
  syncTimeParts();
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
  const nextDateTime = parseDateTime(inputValue.value, props.showSeconds);
  if (inputValue.value.trim() === '') {
    commitValue(null);
    return;
  }

  if (nextDateTime) {
    commitValue(nextDateTime);
    pendingDateTime.value = cloneDate(nextDateTime);
    displayDate.value = startOfMonth(nextDateTime);
    syncTimeParts();
  } else {
    inputValue.value = model.value;
  }
}

function updatePendingDate(date: Date) {
  pendingDateTime.value = setDateTimeParts(
    date,
    Number(pendingHours.value),
    Number(pendingMinutes.value),
    props.showSeconds ? Number(pendingSeconds.value) : 0
  );
  displayDate.value = startOfMonth(date);
}

function updatePendingTime() {
  pendingDateTime.value = setDateTimeParts(
    pendingDateTime.value,
    Number(pendingHours.value),
    Number(pendingMinutes.value),
    props.showSeconds ? Number(pendingSeconds.value) : 0
  );
}

function updatePendingHours(hours: string) {
  pendingHours.value = hours;
  updatePendingTime();
}

function updatePendingMinutes(minutes: string) {
  pendingMinutes.value = minutes;
  updatePendingTime();
}

function updatePendingSeconds(seconds: string) {
  pendingSeconds.value = seconds;
  updatePendingTime();
}

function selectNow() {
  const nextDate = today();
  pendingDateTime.value = nextDate;
  displayDate.value = startOfMonth(nextDate);
  syncTimeParts();
}

function applySelection() {
  updatePendingTime();
  commitValue(pendingDateTime.value);
  closePopover();
}

function isSelectedDate(date: Date) {
  return isSameDay(date, pendingDateTime.value);
}

function isTodayDate(date: Date) {
  return isSameDay(date, today());
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
            <IconCalendarTime :size="16" stroke-width="1.5" aria-hidden="true" />
          </button>
          <div v-if="showTooltip" class="field__tooltip" role="tooltip">
            {{ error }}
          </div>

        <div v-if="isOpen" class="picker__popover">
          <div class="picker__header">
            <div class="picker__calendar-header">
              <button type="button" class="picker__nav-button" aria-label="Năm trước" @click="displayDate = startOfMonth(addYears(displayDate, -1))">
                <IconChevronsLeft :size="16" stroke-width="1.5" aria-hidden="true" />
              </button>
              <button type="button" class="picker__nav-button" aria-label="Tháng trước" @click="displayDate = startOfMonth(addMonths(displayDate, -1))">
                <IconChevronLeft :size="16" stroke-width="1.5" aria-hidden="true" />
              </button>

              <div class="picker__title">
                <span>{{ `Thg ${displayDate.getMonth() + 1}` }}</span>
                <span>{{ displayDate.getFullYear() }}</span>
              </div>

              <button type="button" class="picker__nav-button" aria-label="Tháng sau" @click="displayDate = startOfMonth(addMonths(displayDate, 1))">
                <IconChevronRight :size="16" stroke-width="1.5" aria-hidden="true" />
              </button>
              <button type="button" class="picker__nav-button" aria-label="Năm sau" @click="displayDate = startOfMonth(addYears(displayDate, 1))">
                <IconChevronsRight :size="16" stroke-width="1.5" aria-hidden="true" />
              </button>
            </div>

            <div class="picker__header-time">{{ currentTimeLabel }}</div>
          </div>

          <div class="picker__divider"></div>

          <div class="picker__body">
            <div class="picker__calendar">
              <div class="picker__weekdays">
                <span
                  v-for="(weekday, index) in weekdayLabels"
                  :key="weekday"
                  class="picker__weekday"
                  :class="{ 'picker__weekday--weekend': index === 6 }"
                >
                  {{ weekday }}
                </span>
              </div>

              <div class="picker__days">
                <template v-for="(week, weekIndex) in calendar.rows" :key="weekIndex">
                  <button
                    v-for="date in week"
                    :key="formatDate(date)"
                    type="button"
                    class="picker__day"
                    :class="{
                      'picker__day--outside': !isSameMonth(date, displayDate),
                      'picker__day--today': isTodayDate(date),
                      'picker__day--selected': isSelectedDate(date)
                    }"
                    @click="updatePendingDate(date)"
                  >
                    {{ date.getDate() }}
                  </button>
                </template>
              </div>
            </div>

            <div class="picker__time-columns">
              <div class="picker__time-column">
                <button
                  v-for="option in hourOptions"
                  :key="`hour-${option}`"
                  type="button"
                  class="picker__time-item"
                  :class="{ 'picker__time-item--selected': pendingHours === option }"
                  @click="updatePendingHours(option)"
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
                  @click="updatePendingMinutes(option)"
                >
                  {{ option }}
                </button>
              </div>

              <div v-if="showSeconds" class="picker__time-column">
                <button
                  v-for="option in secondOptions"
                  :key="`second-${option}`"
                  type="button"
                  class="picker__time-item"
                  :class="{ 'picker__time-item--selected': pendingSeconds === option }"
                  @click="updatePendingSeconds(option)"
                >
                  {{ option }}
                </button>
              </div>
            </div>
          </div>

          <div class="picker__divider"></div>

          <div class="picker__footer">
            <button type="button" class="picker__link" @click="selectNow">Bây giờ</button>
            <BaseButton type="button" @click="applySelection">Đồng ý</BaseButton>
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
  width: max-content;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
}

.picker :deep(.picker-field__input) {
  height: 32px;
  padding: 0 32px 0 12px;
  border-radius: 8px;
  font-size: 13px;
  line-height: 1;
}

.picker :deep(.picker-field__action) {
  right: 8px;
  width: 16px;
  height: 16px;
}

.picker :deep(.picker-field__action svg) {
  width: 16px;
  height: 16px;
}

.picker__popover {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  z-index: 30;
  width: 588px;
  border: 1px solid var(--color-border);
  border-radius: 0 0 14px 14px;
  background: var(--color-surface);
  box-shadow: var(--shadow-surface);
  overflow: hidden;
}

.picker__header {
  display: grid;
  grid-template-columns: 304px minmax(0, 284px);
  align-items: center;
  min-height: 48px;
}

.picker__nav {
  display: flex;
  align-items: center;
  gap: 18px;
}

.picker__nav-button {
  display: inline-grid;
  width: 18px;
  height: 18px;
  place-items: center;
  padding: 0;
  border: 0;
  background: transparent;
  color: #333333;
}

.picker__nav-button :deep(svg) {
  width: 18px;
  height: 18px;
}

.picker__calendar-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 20px;
}

.picker__title,
.picker__header-time {
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
  color: #202124;
}

.picker__title {
  display: inline-flex;
  align-items: center;
  gap: 20px;
  margin: 0 4px;
}

.picker__header-time {
  padding: 0 12px;
  text-align: center;
  color: #111827;
}

.picker__divider {
  height: 1px;
  background: var(--color-border);
}

.picker__body {
  display: grid;
  grid-template-columns: 304px minmax(0, 284px);
  min-height: 272px;
}

.picker__calendar {
  padding: 12px 20px 16px;
}

.picker__weekdays,
.picker__days {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  column-gap: 6px;
  row-gap: 8px;
  text-align: center;
}

.picker__weekdays {
  margin-bottom: 10px;
}

.picker__weekday,
.picker__day {
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.picker__weekday {
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
  color: #202124;
}

.picker__weekday--weekend {
  color: var(--color-danger);
}

.picker__day {
  width: 30px;
  height: 24px;
  margin: 0 auto;
  border: 1px solid transparent;
  border-radius: 8px;
  background: transparent;
  color: #202124;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
}

.picker__day:hover:not(.picker__day--selected) {
  background: #f5f6f8;
}

.picker__day--outside {
  color: #8c8c8c;
}

.picker__day--today {
  background: #f1f2f4;
  color: #202124;
}

.picker__day--selected {
  border-color: var(--color-brand);
  background: var(--color-brand);
  color: #ffffff;
}

.picker__day--selected:hover {
  border-color: var(--color-brand);
  background: var(--color-brand);
  color: #ffffff;
}

.picker__time-columns {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  padding: 8px 12px 12px 0;
}

.picker__time-column {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  max-height: 248px;
  overflow: auto;
  padding: 0 6px;
  border-left: 1px solid var(--color-border);
}

.picker__time-item {
  width: 60px;
  height: 32px;
  flex: 0 0 32px;
  border: 0;
  border-radius: 8px;
  background: transparent;
  color: #202124;
  padding: 0 var(--button-padding-x);
  font-size: var(--font-size-body);
  font-weight: 500;
  line-height: var(--line-height-body);
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
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-brand);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  cursor: pointer;
}

.picker__footer :deep(.button) {
  min-width: 88px;
  height: 32px;
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

@media (max-width: 860px) {
  .picker__popover {
    width: min(588px, calc(100vw - 32px));
  }

  .picker__header {
    grid-template-columns: 1fr;
  }

  .picker__header-time {
    display: none;
  }

  .picker__calendar-header {
    justify-content: center;
    flex-wrap: wrap;
  }

  .picker__body {
    grid-template-columns: 1fr;
  }

  .picker__time-columns {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    border-top: 1px solid var(--color-border);
  }
}
</style>
