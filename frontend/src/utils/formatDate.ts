function parseAsUtc(value: string | Date): Date {
  if (value instanceof Date) {
    return value;
  }

  const normalizedValue = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
  return new Date(normalizedValue);
}

export function formatDate(value: string | Date): string {
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
    timeZone: 'Asia/Ho_Chi_Minh'
  }).format(parseAsUtc(value));
}
