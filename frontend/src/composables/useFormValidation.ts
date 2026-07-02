import { computed, ref } from 'vue';
import { ApiError } from '../api/http';

export const FORM_ERROR = '$form' as const;

export type ValidationRule<TValues extends Record<string, unknown>> = (values: TValues) => Partial<Record<keyof TValues, string>>;
export type ApiErrorTarget<TValues extends Record<string, unknown>> = keyof TValues | typeof FORM_ERROR;
export type ApiErrorMap<TValues extends Record<string, unknown>> = Partial<Record<string, ApiErrorTarget<TValues>>>;

export function useFormValidation<TValues extends Record<string, unknown>>(
  values: TValues,
  rules: ValidationRule<TValues>[]
) {
  const errors = ref<Partial<Record<keyof TValues, string>>>({});
  const formError = ref('');

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

  function setFieldError(fieldName: keyof TValues, message: string) {
    errors.value[fieldName] = message;
  }

  function setFormError(message: string) {
    formError.value = message;
  }

  function clearFieldError(fieldName: keyof TValues) {
    delete errors.value[fieldName];
  }

  function clearFormError() {
    formError.value = '';
  }

  function clearErrors() {
    for (const fieldName of Object.keys(errors.value) as (keyof TValues)[]) {
      delete errors.value[fieldName];
    }

    clearFormError();
  }

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
