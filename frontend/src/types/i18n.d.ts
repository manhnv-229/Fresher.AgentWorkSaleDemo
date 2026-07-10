import type { Locale } from '../i18n';

declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    $t: (key: string, params?: Record<string, string | number>) => string;
    $locale: Locale;
  }
}

export {};
