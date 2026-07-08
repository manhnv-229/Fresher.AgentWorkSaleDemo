export type DateRangeModel = {
  start: string;
  end: string;
};

export type DateRangePreset = {
  label: string;
  value: DateRangeModel;
};

const DAY_IN_MS = 24 * 60 * 60 * 1000;
const WEEKDAY_LABELS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'] as const;
const MONTH_LABELS = ['Thg 1', 'Thg 2', 'Thg 3', 'Thg 4', 'Thg 5', 'Thg 6', 'Thg 7', 'Thg 8', 'Thg 9', 'Thg 10', 'Thg 11', 'Thg 12'] as const;

export function getWeekdayLabels() {
  return WEEKDAY_LABELS;
}

export function getMonthLabels() {
  return MONTH_LABELS;
}

export function cloneDate(date: Date) {
  return new Date(date.getTime());
}

export function startOfDay(date: Date) {
  const nextDate = cloneDate(date);
  nextDate.setHours(0, 0, 0, 0);
  return nextDate;
}

export function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

export function endOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0);
}

export function addDays(date: Date, days: number) {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

export function addMonths(date: Date, months: number) {
  return new Date(date.getFullYear(), date.getMonth() + months, 1);
}

export function addYears(date: Date, years: number) {
  return new Date(date.getFullYear() + years, date.getMonth(), 1);
}

export function isSameDay(firstDate: Date | null | undefined, secondDate: Date | null | undefined) {
  if (!firstDate || !secondDate) {
    return false;
  }

  return firstDate.getFullYear() === secondDate.getFullYear()
    && firstDate.getMonth() === secondDate.getMonth()
    && firstDate.getDate() === secondDate.getDate();
}

export function isSameMonth(firstDate: Date | null | undefined, secondDate: Date | null | undefined) {
  if (!firstDate || !secondDate) {
    return false;
  }

  return firstDate.getFullYear() === secondDate.getFullYear()
    && firstDate.getMonth() === secondDate.getMonth();
}

export function isDateInRange(date: Date, startDate: Date | null | undefined, endDate: Date | null | undefined) {
  if (!startDate || !endDate) {
    return false;
  }

  const time = startOfDay(date).getTime();
  const startTime = startOfDay(startDate).getTime();
  const endTime = startOfDay(endDate).getTime();
  return time >= Math.min(startTime, endTime) && time <= Math.max(startTime, endTime);
}

export function formatDate(date: Date | null | undefined) {
  if (!date) {
    return '';
  }

  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = String(date.getFullYear());
  return `${day}/${month}/${year}`;
}

export function formatMonthYear(date: Date | null | undefined) {
  if (!date) {
    return '';
  }

  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = String(date.getFullYear());
  return `${month}/${year}`;
}

export function formatTime(date: Date | null | undefined, showSeconds = false) {
  if (!date) {
    return '';
  }

  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  const seconds = String(date.getSeconds()).padStart(2, '0');
  return showSeconds ? `${hours}:${minutes}:${seconds}` : `${hours}:${minutes}`;
}

export function formatDateTime(date: Date | null | undefined, showSeconds = false) {
  if (!date) {
    return '';
  }

  return `${formatDate(date)} ${formatTime(date, showSeconds)}`;
}

export function parseDate(value: string) {
  const normalizedValue = value.trim();
  const match = normalizedValue.match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
  if (!match) {
    return null;
  }

  const day = Number(match[1]);
  const month = Number(match[2]) - 1;
  const year = Number(match[3]);
  const date = new Date(year, month, day);

  if (date.getFullYear() !== year || date.getMonth() !== month || date.getDate() !== day) {
    return null;
  }

  return date;
}

export function parseMonthYear(value: string) {
  const normalizedValue = value.trim();
  const match = normalizedValue.match(/^(\d{2})\/(\d{4})$/);
  if (!match) {
    return null;
  }

  const month = Number(match[1]) - 1;
  const year = Number(match[2]);
  if (month < 0 || month > 11) {
    return null;
  }

  return new Date(year, month, 1);
}

export function parseTime(value: string, showSeconds = false) {
  const normalizedValue = value.trim();
  const match = normalizedValue.match(showSeconds ? /^(\d{2}):(\d{2}):(\d{2})$/ : /^(\d{2}):(\d{2})$/);
  if (!match) {
    return null;
  }

  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  const seconds = showSeconds ? Number(match[3]) : 0;

  if (
    Number.isNaN(hours) || Number.isNaN(minutes) || Number.isNaN(seconds)
    || hours < 0 || hours > 23
    || minutes < 0 || minutes > 59
    || seconds < 0 || seconds > 59
  ) {
    return null;
  }

  return { hours, minutes, seconds };
}

export function parseDateTime(value: string, showSeconds = false) {
  const normalizedValue = value.trim();
  const segments = normalizedValue.split(' ');
  if (segments.length !== 2) {
    return null;
  }

  const date = parseDate(segments[0]);
  const time = parseTime(segments[1], showSeconds);
  if (!date || !time) {
    return null;
  }

  date.setHours(time.hours, time.minutes, time.seconds, 0);
  return date;
}

export function getCalendarMatrix(baseDate: Date) {
  const monthStart = startOfMonth(baseDate);
  const monthEnd = endOfMonth(baseDate);
  const dayOffset = (monthStart.getDay() + 6) % 7;
  const firstGridDate = addDays(monthStart, -dayOffset);
  const lastGridDate = addDays(firstGridDate, 41);
  const rows: Date[][] = [];

  let currentDate = firstGridDate;
  while (currentDate <= lastGridDate) {
    const week: Date[] = [];
    for (let index = 0; index < 7; index += 1) {
      week.push(currentDate);
      currentDate = addDays(currentDate, 1);
    }

    rows.push(week);
  }

  return {
    rows,
    monthStart,
    monthEnd
  };
}

export function createTimeOptions(limit: number) {
  return Array.from({ length: limit }, (_, index) => String(index).padStart(2, '0'));
}

export function setDateTimeParts(date: Date, hours: number, minutes: number, seconds = 0) {
  const nextDate = cloneDate(date);
  nextDate.setHours(hours, minutes, seconds, 0);
  return nextDate;
}

export function normalizeDateRange(startDate: Date | null, endDate: Date | null) {
  if (!startDate || !endDate) {
    return { startDate, endDate };
  }

  if (startDate.getTime() <= endDate.getTime()) {
    return { startDate, endDate };
  }

  return { startDate: endDate, endDate: startDate };
}

export function formatRangeLabel(startDate: Date | null, endDate: Date | null, withTime = false, showSeconds = false) {
  if (!startDate && !endDate) {
    return '';
  }

  const formatter = withTime
    ? (date: Date | null) => formatDateTime(date, showSeconds)
    : (date: Date | null) => formatDate(date);

  return `${formatter(startDate)} - ${formatter(endDate)}`.trim();
}

export function today() {
  return new Date();
}

export function nowTime(showSeconds = false) {
  const date = today();
  return formatTime(date, showSeconds);
}

export function toDateRangeModel(startDate: Date | null, endDate: Date | null, withTime = false, showSeconds = false): DateRangeModel {
  if (withTime) {
    return {
      start: formatDateTime(startDate, showSeconds),
      end: formatDateTime(endDate, showSeconds)
    };
  }

  return {
    start: formatDate(startDate),
    end: formatDate(endDate)
  };
}

export function parseRangePart(value: string, withTime = false, showSeconds = false) {
  return withTime ? parseDateTime(value, showSeconds) : parseDate(value);
}

export function differenceInDays(startDate: Date, endDate: Date) {
  return Math.round((startOfDay(endDate).getTime() - startOfDay(startDate).getTime()) / DAY_IN_MS);
}
