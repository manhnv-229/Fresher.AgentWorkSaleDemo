<script setup lang="ts">
import ToastStack from '../components/notifications/ToastStack.vue';
import NotificationStack from '../components/notifications/NotificationStack.vue';
</script>

<template>
  <RouterView />
  <ToastStack />
  <NotificationStack />
</template>
