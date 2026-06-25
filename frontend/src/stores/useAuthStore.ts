import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import type { AuthState, TokenResponse } from '../api/auth.types';

const AUTH_STORAGE_KEY = 'demo.auth';

export const useAuthStore = defineStore('auth', () => {
  const authState = ref<AuthState | null>(readAuthState());
  const isAuthenticated = computed(() => authState.value !== null);

  function setAuthState(tokens: TokenResponse): AuthState {
    const nextAuthState: AuthState = {
      accessToken: tokens.accessToken,
      accessTokenExpiresAt: tokens.accessTokenExpiresAt,
      refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
      receivedAt: new Date().toISOString()
    };

    authState.value = nextAuthState;
    // Pinia là source of truth runtime, còn sessionStorage giữ khả năng khôi phục phiên sau reload.
    sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(nextAuthState));
    return nextAuthState;
  }

  function clearAuthState(): void {
    authState.value = null;
    // Xóa luôn dữ liệu cũ để các lần bootstrap sau không đọc phải phiên stale.
    clearLegacyPersistentAuthState();
  }

  function getAccessToken(): string | null {
    return authState.value?.accessToken ?? null;
  }

  function getAuthState(): AuthState | null {
    return authState.value;
  }

  return {
    authState,
    isAuthenticated,
    setAuthState,
    clearAuthState,
    getAccessToken,
    getAuthState
  };
});

function readAuthState(): AuthState | null {
  try {
    const stored = sessionStorage.getItem(AUTH_STORAGE_KEY);
    if (!stored) {
      return null;
    }

    return JSON.parse(stored) as AuthState;
  } catch {
    // Nếu dữ liệu lưu bị lỗi format thì dọn sạch để tránh khởi tạo store ở trạng thái nửa hợp lệ.
    clearLegacyPersistentAuthState();
    return null;
  }
}

function clearLegacyPersistentAuthState(): void {
  localStorage.removeItem(AUTH_STORAGE_KEY);
  sessionStorage.removeItem(AUTH_STORAGE_KEY);
}
