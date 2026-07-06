<script setup lang="ts">
import AppToastStack from '../components/AppToastStack.vue';
import AppNotificationStack from '../components/AppNotificationStack.vue';
</script>

<template>
  <RouterView />
  <AppToastStack />
  <AppNotificationStack />
</template>
