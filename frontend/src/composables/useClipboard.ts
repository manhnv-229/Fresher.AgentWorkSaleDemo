import { ref } from 'vue';

// Trạng thái copy ngắn hạn cho tooltip hoặc toast kiểu "Đã sao chép".
export function useClipboard() {
  const copied = ref(false);

  // Sao chép text và bật cờ copied trong một khoảng ngắn để UI phản hồi.
  async function copy(text: string) {
    await navigator.clipboard.writeText(text);
    copied.value = true;
    window.setTimeout(() => {
      copied.value = false;
    }, 1500);
  }

  return {
    copied,
    copy
  };
}
