export function isRequired(value: string): boolean {
  return value.trim().length > 0;
}

export function isEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim());
}

export function hasMinLength(value: string, minLength: number): boolean {
  return value.trim().length >= minLength;
}

export function hasMaxLength(value: string, maxLength: number): boolean {
  return value.trim().length <= maxLength;
}

export function isOneOf<TValue extends string>(value: string, allowedValues: readonly TValue[]): boolean {
  return allowedValues.includes(value.trim() as TValue);
}

export function hasAllowedFileExtension(fileName: string, allowedExtensions: readonly string[]): boolean {
  const normalizedFileName = fileName.trim().toLowerCase();
  return allowedExtensions.some((extension) => normalizedFileName.endsWith(extension.toLowerCase()));
}

export function hasPositiveFileSize(size: number): boolean {
  return size > 0;
}

export function hasMaxFileSize(size: number, maxBytes: number): boolean {
  return size <= maxBytes;
}
