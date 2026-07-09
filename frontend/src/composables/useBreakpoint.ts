import { computed, onMounted, onUnmounted, ref } from 'vue';

// Theo dõi viewport width để các component đơn giản có thể phản ứng theo breakpoint.
export function useBreakpoint() {
  const width = ref(typeof window === 'undefined' ? 0 : window.innerWidth);
  const isMobile = computed(() => width.value < 768);

  // Cập nhật width runtime khi người dùng resize trình duyệt.
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
