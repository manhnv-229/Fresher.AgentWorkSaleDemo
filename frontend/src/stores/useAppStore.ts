import { ref } from 'vue';

const appName = ref('Demo');

export function useAppStore() {
  return {
    appName
  };
}
