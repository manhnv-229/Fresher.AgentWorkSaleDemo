import { computed, ref } from 'vue';

export type ValidationRule<TValues extends Record<string, string>> = (values: TValues) => Partial<Record<keyof TValues, string>>;

export function useFormValidation<TValues extends Record<string, string>>(
  values: TValues,
  rules: ValidationRule<TValues>[]
) {
  const errors = ref<Partial<Record<keyof TValues, string>>>({});

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

  function clearFieldError(fieldName: keyof TValues) {
    delete errors.value[fieldName];
  }

  function clearErrors() {
    for (const fieldName of Object.keys(errors.value) as (keyof TValues)[]) {
      delete errors.value[fieldName];
    }
  }

  const hasErrors = computed(() => Object.keys(errors.value).length > 0);

  return {
    errors,
    hasErrors,
    validate,
    setFieldError,
    clearFieldError,
    clearErrors
  };
}
