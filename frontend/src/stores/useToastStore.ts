import { computed, ref } from 'vue';
import { defineStore } from 'pinia';

export type ToastTone = 'success' | 'error' | 'warning' | 'info';

export interface ToastItem {
  id: number;
  title: string;
  message?: string;
  tone: ToastTone;
}

const MAX_VISIBLE_TOASTS = 3;
const AUTO_CLOSE_DELAY = 5000;

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<ToastItem[]>([]);
  const nextId = ref(1);
  const timers = new Map<number, number>();

  const visibleToasts = computed(() => toasts.value.slice(0, MAX_VISIBLE_TOASTS));

  function push(toast: Omit<ToastItem, 'id'>) {
    const item: ToastItem = {
      id: nextId.value++,
      ...toast
    };

    toasts.value = [item, ...toasts.value].slice(0, MAX_VISIBLE_TOASTS);
    scheduleRemoval(item.id);
    return item.id;
  }

  function remove(id: number) {
    toasts.value = toasts.value.filter((item) => item.id !== id);
    const timer = timers.get(id);
    if (timer) {
      window.clearTimeout(timer);
      timers.delete(id);
    }
  }

  function clear() {
    for (const timer of timers.values()) {
      window.clearTimeout(timer);
    }

    timers.clear();
    toasts.value = [];
  }

  function scheduleRemoval(id: number) {
    const existingTimer = timers.get(id);
    if (existingTimer) {
      window.clearTimeout(existingTimer);
    }

    const timer = window.setTimeout(() => {
      remove(id);
    }, AUTO_CLOSE_DELAY);

    timers.set(id, timer);
  }

  return {
    toasts,
    visibleToasts,
    push,
    remove,
    clear
  };
});
