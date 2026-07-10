import { computed, inject, provide, ref, type ComputedRef } from 'vue';
import { messages } from './messages';

export type Locale = keyof typeof messages;

export const I18N_KEY = Symbol('i18n');

function resolveMessage(path: string, locale: Locale): string {
  const parts = path.split('.');
  let current: unknown = messages[locale];

  for (const part of parts) {
    if (!current || typeof current !== 'object' || !(part in current)) {
      return path;
    }

    current = (current as Record<string, unknown>)[part];
  }

  return typeof current === 'string' ? current : path;
}

function interpolate(message: string, params?: Record<string, string | number>) {
  if (!params) {
    return message;
  }

  return Object.entries(params).reduce(
    (output, [key, value]) => output.split(`{${key}}`).join(String(value)),
    message
  );
}

export function createI18n(defaultLocale: Locale = 'vi') {
  const locale = ref<Locale>(defaultLocale);

  function t(key: string, params?: Record<string, string | number>) {
    return interpolate(resolveMessage(key, locale.value), params);
  }

  return {
    locale,
    t
  };
}

export function provideI18n() {
  const i18n = createI18n();
  provide(I18N_KEY, i18n);
  return i18n;
}

export function useI18n() {
  const i18n = inject<{ locale: ComputedRef<Locale> | { value: Locale }; t: (key: string, params?: Record<string, string | number>) => string }>(
    I18N_KEY
  );

  if (!i18n) {
    throw new Error('i18n has not been provided');
  }

  return i18n;
}

export { messages };
