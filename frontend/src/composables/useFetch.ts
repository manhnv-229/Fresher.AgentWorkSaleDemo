import { ref } from 'vue';

// Wrapper nhỏ cho pattern load/error/data của một request bất kỳ.
export function useFetch<TData>() {
  const data = ref<TData | null>(null);
  const error = ref<Error | null>(null);
  const isLoading = ref(false);

  // Nhận vào callback request để composable không phụ thuộc vào bất kỳ API cụ thể nào.
  async function execute(request: () => Promise<TData>) {
    isLoading.value = true;
    error.value = null;
    try {
      data.value = await request();
    } catch (caughtError) {
      error.value = caughtError instanceof Error ? caughtError : new Error('Request failed.');
    } finally {
      isLoading.value = false;
    }
  }

  return {
    data,
    error,
    isLoading,
    execute
  };
}
