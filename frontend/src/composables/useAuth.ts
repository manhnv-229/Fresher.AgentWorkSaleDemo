import { computed, ref, type ComputedRef, type DeepReadonly, type Ref } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { changePassword as changePasswordRequest, login as loginRequest, logout as logoutRequest, refreshAccessToken } from '../api';
import { useAuthStore } from '../stores/useAuthStore';
import type { AuthState } from '../api/auth.types';

// API bề mặt của composable auth dùng cho login page, layout và settings password.
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

// Cờ shared cho toàn app để tránh nhiều component cùng bootstrap auth lặp lại.
const isInitializing = ref(false);
let startupRefreshAttempted = false;

// Gom toàn bộ luồng auth runtime: login, refresh, logout và bootstrap phiên.
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

  // Đăng nhập rồi đồng bộ access token + permission claims vào store.
  async function login(email: string, password: string) {
    const tokens = await loginRequest({ email, password });
    authStore.setAuthState(tokens);
  }

  // Làm mới access token từ refresh token đang nằm ở cookie HttpOnly.
  async function refresh() {
    const tokens = await refreshAccessToken();
    authStore.setAuthState(tokens);
  }

  // Đổi mật khẩu xong thì buộc dọn phiên hiện tại phía client để đồng bộ với backend revoke session.
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

  // Bootstrap auth state khi app khởi động hoặc sau reload trình duyệt.
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

  // Gọi logout backend nếu có thể, nhưng luôn dọn state local kể cả khi request lỗi.
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

// So sánh mốc hết hạn từ backend với thời điểm hiện tại phía client.
function isExpired(expiresAt: string): boolean {
  const expiresAtTimestamp = Date.parse(expiresAt);
  if (Number.isNaN(expiresAtTimestamp)) {
    return true;
  }

  return expiresAtTimestamp <= Date.now();
}
