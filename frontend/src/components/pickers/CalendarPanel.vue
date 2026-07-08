<script setup lang="ts">
import { computed } from 'vue';
import {
  addMonths,
  addYears,
  formatDate,
  getCalendarMatrix,
  getWeekdayLabels,
  isDateInRange,
  isSameDay,
  isSameMonth
} from '../../utils/datePicker';
import {
  IconChevronsLeft,
  IconChevronLeft,
  IconChevronRight,
  IconChevronsRight
} from '@tabler/icons-vue';

const props = withDefaults(
  defineProps<{
    displayDate: Date;
    selectedDate?: Date | null;
    rangeStart?: Date | null;
    rangeEnd?: Date | null;
    pendingDate?: Date | null;
    actionLabel?: string;
  }>(),
  {
    selectedDate: null,
    rangeStart: null,
    rangeEnd: null,
    pendingDate: null,
    actionLabel: 'Hôm nay'
  }
);

const emit = defineEmits<{
  select: [date: Date];
  navigateMonth: [date: Date];
  navigateYear: [date: Date];
  action: [];
}>();

const calendar = computed(() => getCalendarMatrix(props.displayDate));
const weekdayLabels = computed(() => getWeekdayLabels());
const panelTitle = computed(() => `Thg ${props.displayDate.getMonth() + 1}`);
const panelYear = computed(() => props.displayDate.getFullYear());

function selectDate(date: Date) {
  emit('select', date);
}

function isSelected(date: Date) {
  return isSameDay(date, props.selectedDate) || isSameDay(date, props.pendingDate);
}

function isRangeStart(date: Date) {
  return isSameDay(date, props.rangeStart);
}

function isRangeEnd(date: Date) {
  return isSameDay(date, props.rangeEnd);
}

function isRangeBetween(date: Date) {
  return isDateInRange(date, props.rangeStart, props.rangeEnd);
}
</script>

<template>
  <div class="calendar-panel">
    <div class="calendar-panel__header">
      <div class="calendar-panel__nav">
        <button type="button" class="calendar-panel__nav-button" aria-label="Năm trước" @click="emit('navigateYear', addYears(displayDate, -1))">
          <IconChevronsLeft :size="20" stroke-width="1.5" aria-hidden="true" />
        </button>
        <button type="button" class="calendar-panel__nav-button" aria-label="Tháng trước" @click="emit('navigateMonth', addMonths(displayDate, -1))">
          <IconChevronLeft :size="20" stroke-width="1.5" aria-hidden="true" />
        </button>
      </div>

      <div class="calendar-panel__title">
        <span>{{ panelTitle }}</span>
        <span>{{ panelYear }}</span>
      </div>

      <div class="calendar-panel__nav">
        <button type="button" class="calendar-panel__nav-button" aria-label="Tháng sau" @click="emit('navigateMonth', addMonths(displayDate, 1))">
          <IconChevronRight :size="20" stroke-width="1.5" aria-hidden="true" />
        </button>
        <button type="button" class="calendar-panel__nav-button" aria-label="Năm sau" @click="emit('navigateYear', addYears(displayDate, 1))">
          <IconChevronsRight :size="20" stroke-width="1.5" aria-hidden="true" />
        </button>
      </div>
    </div>

    <div class="calendar-panel__divider"></div>

    <div class="calendar-panel__weekdays">
      <span
        v-for="(weekday, index) in weekdayLabels"
        :key="weekday"
        class="calendar-panel__weekday"
        :class="{ 'calendar-panel__weekday--weekend': index === 6 }"
      >
        {{ weekday }}
      </span>
    </div>

    <div class="calendar-panel__days">
      <template v-for="(week, weekIndex) in calendar.rows" :key="weekIndex">
        <button
          v-for="date in week"
          :key="formatDate(date)"
          type="button"
          class="calendar-panel__day"
          :class="{
            'calendar-panel__day--outside': !isSameMonth(date, displayDate),
            'calendar-panel__day--selected': isSelected(date),
            'calendar-panel__day--range': isRangeBetween(date),
            'calendar-panel__day--range-start': isRangeStart(date),
            'calendar-panel__day--range-end': isRangeEnd(date)
          }"
          @click="selectDate(date)"
        >
          {{ date.getDate() }}
        </button>
      </template>
    </div>

    <div class="calendar-panel__divider"></div>

    <div v-if="actionLabel" class="calendar-panel__footer">
      <button type="button" class="calendar-panel__action" @click="emit('action')">
        {{ actionLabel }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.calendar-panel {
  display: grid;
  min-width: 296px;
  background: var(--color-surface);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
}

.calendar-panel__header,
.calendar-panel__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 48px;
  padding: 0 16px;
}

.calendar-panel__nav {
  display: flex;
  align-items: center;
  gap: 4px;
}

.calendar-panel__nav-button {
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

.calendar-panel__nav-button:hover {
  background: var(--color-brand-soft);
  color: var(--color-text);
}

.calendar-panel__title {
  display: inline-flex;
  align-items: center;
  gap: 12px;
  color: var(--color-text);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

.calendar-panel__divider {
  height: 1px;
  background: var(--color-border);
}

.calendar-panel__weekdays,
.calendar-panel__days {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  padding: 12px;
}

.calendar-panel__weekdays {
  gap: 6px;
  padding-bottom: 8px;
}

.calendar-panel__days {
  gap: 0;
  padding-top: 8px;
}

.calendar-panel__weekday,
.calendar-panel__day {
  display: inline-flex;
  height: 32px;
  align-items: center;
  justify-content: center;
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
}

.calendar-panel__weekday {
  color: var(--color-text);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

.calendar-panel__weekday--weekend {
  color: var(--color-danger);
}

.calendar-panel__day {
  width: 100%;
  border: 0;
  border-radius: 0;
  background: transparent;
  color: var(--color-text);
}

.calendar-panel__day:hover:not(.calendar-panel__day--selected):not(.calendar-panel__day--range-start):not(.calendar-panel__day--range-end) {
  background: var(--color-brand-soft);
}

.calendar-panel__day--outside {
  color: var(--color-text-placeholder);
}

.calendar-panel__day--range {
  background: var(--color-brand-soft);
}

.calendar-panel__day--range-start,
.calendar-panel__day--range-end {
  background: var(--color-brand);
  color: #ffffff;
}

.calendar-panel__day--selected {
  background: var(--color-brand);
  color: #ffffff;
}

.calendar-panel__day--selected:hover,
.calendar-panel__day--range-start:hover,
.calendar-panel__day--range-end:hover {
  background: var(--color-brand);
  color: #ffffff;
}

.calendar-panel__day--range-start {
  border-radius: var(--button-radius);
}

.calendar-panel__day--range-end {
  border-radius: var(--button-radius);
}

.calendar-panel__day--range-start.calendar-panel__day--range-end,
.calendar-panel__day--selected {
  border-radius: var(--button-radius);
}

.calendar-panel__action {
  width: 100%;
  min-height: 32px;
  border: 0;
  background: transparent;
  color: var(--color-brand);
  font-size: var(--font-size-body);
  line-height: var(--line-height-body);
  font-weight: 500;
}

.calendar-panel__action:hover {
  color: var(--color-brand-hover);
}
</style>
