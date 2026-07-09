import { computed, onBeforeUnmount, onMounted, ref, type Ref } from 'vue';
import { onBeforeRouteLeave, useRouter, type RouteLocationRaw } from 'vue-router';

// Điều kiện tối thiểu để guard biết khi nào phải cảnh báo rời trang.
interface UseUnsavedChangesGuardOptions {
  isDirty: Ref<boolean>;
  isSubmitting?: Ref<boolean>;
}

// Guard dùng chung cho các form có state chỉnh sửa chưa lưu.
export function useUnsavedChangesGuard(options: UseUnsavedChangesGuardOptions) {
  const router = useRouter();
  const isDialogOpen = ref(false);
  const pendingRoute = ref<RouteLocationRaw | null>(null);

  // Không cảnh báo khi form đang submit để tránh chặn điều hướng hợp lệ sau lưu.
  const shouldWarn = computed(() => options.isDirty.value && !(options.isSubmitting?.value ?? false));

  // Chặn đóng tab hoặc reload browser khi còn thay đổi chưa lưu.
  function handleBeforeUnload(event: BeforeUnloadEvent) {
    if (!shouldWarn.value) {
      return;
    }

    event.preventDefault();
    event.returnValue = '';
  }

  // Đóng dialog và giữ người dùng ở lại trang hiện tại.
  function stayOnPage() {
    pendingRoute.value = null;
    isDialogOpen.value = false;
  }

  // Bỏ thay đổi và tiếp tục điều hướng đến route đã bị chặn trước đó.
  async function discardChanges() {
    const target = pendingRoute.value;
    pendingRoute.value = null;
    isDialogOpen.value = false;

    if (target) {
      await router.push(target);
    }
  }

  // beforeunload chỉ được gắn khi component đang sống để tránh side effect toàn cục kéo dài.
  onMounted(() => {
    window.addEventListener('beforeunload', handleBeforeUnload);
  });

  onBeforeUnmount(() => {
    window.removeEventListener('beforeunload', handleBeforeUnload);
  });

  // Route guard nội bộ của trang, bật dialog thay vì cho điều hướng rời đi ngay.
  onBeforeRouteLeave((to) => {
    if (!shouldWarn.value) {
      return true;
    }

    pendingRoute.value = to.fullPath;
    isDialogOpen.value = true;
    return false;
  });

  return {
    isDialogOpen,
    discardChanges,
    stayOnPage
  };
}
