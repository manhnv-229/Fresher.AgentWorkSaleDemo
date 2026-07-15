// Kiểm tra chuỗi có nội dung thực sau khi bỏ khoảng trắng đầu/cuối.
export function isRequired(value: string): boolean {
  return value.trim().length > 0;
}

// Kiểm tra nhanh định dạng email dùng cho validation phía client.
export function isEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

// Đảm bảo độ dài chuỗi sau khi loại bỏ khoảng trắng bao quanh không nhỏ hơn giới hạn.
export function hasMinLength(value: string, minLength: number): boolean {
  return value.trim().length >= minLength;
}

// Đảm bảo độ dài chuỗi sau khi loại bỏ khoảng trắng bao quanh không vượt giới hạn.
export function hasMaxLength(value: string, maxLength: number): boolean {
  return value.trim().length <= maxLength;
}

// Kiểm tra giá trị đã trim có nằm trong tập giá trị hợp lệ hay không.
export function isOneOf<TValue extends string>(value: string, allowedValues: readonly TValue[]): boolean {
  return allowedValues.includes(value.trim() as TValue);
}

// Kiểm tra phần đuôi file không phân biệt hoa thường trước khi upload.
export function hasAllowedFileExtension(fileName: string, allowedExtensions: readonly string[]): boolean {
  const normalizedFileName = fileName.trim().toLowerCase();
  return allowedExtensions.some((extension) => normalizedFileName.endsWith(extension.toLowerCase()));
}

// Từ chối file rỗng vì không có nội dung để upload hoặc preview.
export function hasPositiveFileSize(size: number): boolean {
  return size > 0;
}

// Kiểm tra file không vượt giới hạn kích thước backend/UI cho phép.
export function hasMaxFileSize(size: number, maxBytes: number): boolean {
  return size <= maxBytes;
}
