// Chuẩn hóa timestamp thiếu timezone thành UTC trước khi format theo múi giờ Việt Nam.
function parseAsUtc(value: string | Date): Date {
  if (value instanceof Date) {
    return value;
  }

  const normalizedValue = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
  return new Date(normalizedValue);
}

// Hiển thị ngày giờ từ API theo locale vi-VN và múi giờ Asia/Ho_Chi_Minh.
export function formatDate(value: string | Date): string {
  return new Intl.DateTimeFormat('vi-VN', {
    dateStyle: 'short',
    timeStyle: 'short',
    timeZone: 'Asia/Ho_Chi_Minh'
  }).format(parseAsUtc(value));
}
