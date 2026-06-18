import { computed, onMounted, onUnmounted, ref } from 'vue';

export function useBreakpoint() {
  const width = ref(typeof window === 'undefined' ? 0 : window.innerWidth);
  const isMobile = computed(() => width.value < 768);

  function updateWidth() {
    width.value = window.innerWidth;
  }

  onMounted(() => window.addEventListener('resize', updateWidth));
  onUnmounted(() => window.removeEventListener('resize', updateWidth));

  return {
    width,
    isMobile
  };
}
