import { computed, ref, type ComputedRef, type DeepReadonly, type Ref } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { changePassword as changePasswordRequest, login as loginRequest, logout as logoutRequest, refreshAccessToken } from '../api';
import { useAuthStore } from '../stores/useAuthStore';
import type { AuthState } from '../api/auth.types';

export interface UseAuthResult {
  authState: DeepReadonly<Ref<AuthState | null>>;
  isAuthenticated: ComputedRef<boolean>;
  isInitializing: Ref<boolean>;
  accessTokenPreview: ComputedRef<string>;
  initializeAuth: () => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  refresh: () => Promise<void>;
  logout: () => Promise<void>;
  clearSession: () => void;
}

const isInitializing = ref(false);
let startupRefreshAttempted = false;

export function useAuth(): UseAuthResult {
  const authStore = useAuthStore();
  const router = useRouter();
  const { authState } = storeToRefs(authStore);
  const isAuthenticated = computed(() => authStore.isAuthenticated);
  const accessTokenPreview = computed(() => {
    const token = authState.value?.accessToken;
    if (!token) {
      return '';
    }

    return `${token.slice(0, 18)}...${token.slice(-12)}`;
  });

  async function login(email: string, password: string) {
    const tokens = await loginRequest({ email, password });
    authStore.setAuthState(tokens);
  }

  async function refresh() {
    const tokens = await refreshAccessToken();
    authStore.setAuthState(tokens);
  }

  async function changePassword(currentPassword: string, newPassword: string) {
    await changePasswordRequest({ currentPassword, newPassword });
    // Sau khi đổi mật khẩu, frontend chủ động xóa phiên hiện tại để buộc đăng nhập lại bằng thông tin mới.
    authStore.clearAuthState();
    // Điều hướng đến trang login 
    const currentPath = router.currentRoute.value.fullPath;
    router.replace({
      name: 'login',
      query: { reason: 'password-changed', redirect: currentPath }
    });
  }

  async function initializeAuth() {
    if (startupRefreshAttempted) {
      // Chỉ thử khôi phục phiên một lần khi khởi động để tránh refresh lặp vô hạn.
      return;
    }

    startupRefreshAttempted = true;
    isInitializing.value = true;
    try {
      const currentAuthState = authStore.getAuthState();
      if (currentAuthState && !isExpired(currentAuthState.accessTokenExpiresAt)) {
        // Nếu access token vẫn còn hạn thì giữ nguyên state hiện tại thay vì gọi refresh thừa.
        return;
      }

      await refresh();
    } catch {
      // Khi refresh thất bại, frontend dọn trạng thái cục bộ để router xử lý như phiên hết hạn.
      authStore.clearAuthState();
    } finally {
      isInitializing.value = false;
    }
  }

  async function logout() {
    try {
      await logoutRequest();
    } finally {
      authStore.clearAuthState();
    }
  }

  return {
    authState,
    isAuthenticated,
    isInitializing,
    accessTokenPreview,
    initializeAuth,
    login,
    changePassword,
    refresh,
    logout,
    clearSession: authStore.clearAuthState
  };
}

function isExpired(expiresAt: string): boolean {
  const expiresAtTimestamp = Date.parse(expiresAt);
  if (Number.isNaN(expiresAtTimestamp)) {
    return true;
  }

  return expiresAtTimestamp <= Date.now();
}
