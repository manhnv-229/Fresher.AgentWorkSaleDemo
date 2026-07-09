import { computed, ref } from 'vue';
import { ApiError } from '../api/http';

// Target đặc biệt cho lỗi mức form, không gắn vào field cụ thể.
export const FORM_ERROR = '$form' as const;

// Mỗi rule trả ra tập lỗi field tương ứng với state hiện tại của form.
export type ValidationRule<TValues extends Record<string, unknown>> = (values: TValues) => Partial<Record<keyof TValues, string>>;
export type ApiErrorTarget<TValues extends Record<string, unknown>> = keyof TValues | typeof FORM_ERROR;
export type ApiErrorMap<TValues extends Record<string, unknown>> = Partial<Record<string, ApiErrorTarget<TValues>>>;

// Composable validation tối giản cho form local + mapping lỗi từ backend.
export function useFormValidation<TValues extends Record<string, unknown>>(
  values: TValues,
  rules: ValidationRule<TValues>[]
) {
  const errors = ref<Partial<Record<keyof TValues, string>>>({});
  const formError = ref('');

  // Chạy toàn bộ rule và chỉ giữ message đầu tiên của từng field.
  function validate(): boolean {
    clearErrors();

    for (const rule of rules) {
      const result = rule(values);
      for (const [fieldName, message] of Object.entries(result) as [keyof TValues, string][]) {
        if (message && !errors.value[fieldName]) {
          errors.value[fieldName] = message;
        }
      }
    }

    return Object.keys(errors.value).length === 0;
  }

  // Gán lỗi thủ công cho một field, thường dùng khi map lỗi backend cụ thể.
  function setFieldError(fieldName: keyof TValues, message: string) {
    errors.value[fieldName] = message;
  }

  function setFormError(message: string) {
    formError.value = message;
  }

  // Xóa lỗi của một field khi người dùng bắt đầu sửa lại dữ liệu.
  function clearFieldError(fieldName: keyof TValues) {
    delete errors.value[fieldName];
  }

  function clearFormError() {
    formError.value = '';
  }

  // Dọn toàn bộ state lỗi trước một lượt validate hoặc submit mới.
  function clearErrors() {
    for (const fieldName of Object.keys(errors.value) as (keyof TValues)[]) {
      delete errors.value[fieldName];
    }

    clearFormError();
  }

  // Chuyển ApiError sang field error hoặc form error theo codeMap cấu hình tại màn hình gọi.
  function applyApiError(error: unknown, codeMap?: ApiErrorMap<TValues>, fallbackMessage = 'Thao tác không thành công.') {
    const message = error instanceof Error ? error.message : fallbackMessage;

    if (error instanceof ApiError) {
      const target = error.body?.code ? codeMap?.[error.body.code] : undefined;
      if (target && target !== FORM_ERROR) {
        setFieldError(target, message);
        return;
      }

      setFormError(message);
      return;
    }

    setFormError(message);
  }

  // Dùng để bật/tắt trạng thái UI như summary error hoặc disable submit.
  const hasErrors = computed(() => Object.keys(errors.value).length > 0);

  return {
    errors,
    formError,
    hasErrors,
    validate,
    setFieldError,
    setFormError,
    clearFieldError,
    clearFormError,
    clearErrors,
    applyApiError
  };
}
