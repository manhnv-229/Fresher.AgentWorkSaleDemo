import { computed, ref, type ComputedRef, type DeepReadonly, type Ref } from 'vue';
import { changePassword as changePasswordRequest, login as loginRequest, logout as logoutRequest, refreshAccessToken } from '../api';
import { clearAuthState, getAccessToken, getAuthState, readonlyAuthState, setAuthState } from '../stores/auth';
import { setAccessTokenProvider } from '../api/interceptors';
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

// Liên kết tầng HTTP với access token hiện có trong store mà không tạo phụ thuộc vòng.
setAccessTokenProvider(getAccessToken);

export function useAuth(): UseAuthResult {
  const authState = readonlyAuthState;
  const isAuthenticated = computed(() => authState.value !== null);
  const accessTokenPreview = computed(() => {
    const token = authState.value?.accessToken;
    if (!token) {
      return '';
    }

    return `${token.slice(0, 18)}...${token.slice(-12)}`;
  });

  async function login(email: string, password: string) {
    const tokens = await loginRequest({ email, password });
    setAuthState(tokens);
  }

  async function refresh() {
    const tokens = await refreshAccessToken();
    setAuthState(tokens);
  }

  async function changePassword(currentPassword: string, newPassword: string) {
    await changePasswordRequest({ currentPassword, newPassword });
    clearAuthState();
  }

  async function initializeAuth() {
    if (startupRefreshAttempted) {
      // Chỉ thử khôi phục phiên một lần khi khởi động để tránh refresh lặp vô hạn.
      return;
    }

    startupRefreshAttempted = true;
    isInitializing.value = true;
    try {
      const currentAuthState = getAuthState();
      if (currentAuthState && !isExpired(currentAuthState.accessTokenExpiresAt)) {
        // Nếu access token vẫn còn hạn thì giữ nguyên state hiện tại thay vì gọi refresh thừa.
        return;
      }

      await refresh();
    } catch {
      // Khi refresh thất bại, frontend dọn trạng thái cục bộ để router xử lý như phiên hết hạn.
      clearAuthState();
    } finally {
      isInitializing.value = false;
    }
  }

  async function logout() {
    try {
      await logoutRequest();
    } finally {
      clearAuthState();
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
    clearSession: clearAuthState
  };
}

function isExpired(expiresAt: string): boolean {
  const expiresAtTimestamp = Date.parse(expiresAt);
  if (Number.isNaN(expiresAtTimestamp)) {
    return true;
  }

  return expiresAtTimestamp <= Date.now();
}
