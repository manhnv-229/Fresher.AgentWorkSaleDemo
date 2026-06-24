import { ref } from 'vue';
import { defineStore } from 'pinia';

export const useAppStore = defineStore('app', () => {
  const appName = ref('Demo');

  return {
    appName
  };
});
