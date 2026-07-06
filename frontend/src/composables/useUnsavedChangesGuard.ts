import { computed, onBeforeUnmount, onMounted, ref, type Ref } from 'vue';
import { onBeforeRouteLeave, useRouter, type RouteLocationRaw } from 'vue-router';

interface UseUnsavedChangesGuardOptions {
  isDirty: Ref<boolean>;
  isSubmitting?: Ref<boolean>;
}

export function useUnsavedChangesGuard(options: UseUnsavedChangesGuardOptions) {
  const router = useRouter();
  const isDialogOpen = ref(false);
  const pendingRoute = ref<RouteLocationRaw | null>(null);

  const shouldWarn = computed(() => options.isDirty.value && !(options.isSubmitting?.value ?? false));

  function handleBeforeUnload(event: BeforeUnloadEvent) {
    if (!shouldWarn.value) {
      return;
    }

    event.preventDefault();
    event.returnValue = '';
  }

  function stayOnPage() {
    pendingRoute.value = null;
    isDialogOpen.value = false;
  }

  async function discardChanges() {
    const target = pendingRoute.value;
    pendingRoute.value = null;
    isDialogOpen.value = false;

    if (target) {
      await router.push(target);
    }
  }

  onMounted(() => {
    window.addEventListener('beforeunload', handleBeforeUnload);
  });

  onBeforeUnmount(() => {
    window.removeEventListener('beforeunload', handleBeforeUnload);
  });

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
