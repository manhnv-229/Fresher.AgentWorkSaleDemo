<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { useNotificationStore, type NotificationItem } from '../../stores/useNotificationStore';
import Notification from './Notification.vue';

const notificationStore = useNotificationStore();
const { visibleNotifications } = storeToRefs(notificationStore);
</script>

<template>
  <Teleport to="body">
    <div v-if="visibleNotifications.length > 0" class="notification-stack" aria-live="polite" aria-atomic="false">
      <component
        v-for="notification in visibleNotifications"
        :key="notification.id"
        :is="Notification"
        :tone="notification.tone"
        :title="notification.title"
        :message="notification.message"
        @close="notificationStore.remove(notification.id)"
      />
    </div>
  </Teleport>
</template>
