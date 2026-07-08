import { computed, ref } from 'vue';
import { defineStore } from 'pinia';

export type NotificationTone = 'success' | 'error' | 'warning' | 'info';

export interface NotificationItem {
  id: number;
  title: string;
  message?: string;
  tone: NotificationTone;
}

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref<NotificationItem[]>([]);
  const nextId = ref(1);

  const visibleNotifications = computed(() => notifications.value);

  function push(notification: Omit<NotificationItem, 'id'>) {
    const item: NotificationItem = {
      id: nextId.value++,
      ...notification
    };

    notifications.value = [item, ...notifications.value];
    return item.id;
  }

  function remove(id: number) {
    notifications.value = notifications.value.filter((item) => item.id !== id);
  }

  function clear() {
    notifications.value = [];
  }

  return {
    notifications,
    visibleNotifications,
    push,
    remove,
    clear
  };
});
