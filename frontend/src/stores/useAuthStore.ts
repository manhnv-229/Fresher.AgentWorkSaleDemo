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
      receivedAt: new Date().toISOString(),
      permissions: extractPermissions(tokens.accessToken)
    };

    authState.value = nextAuthState;
    // Pinia là source of truth runtime; sessionStorage chỉ giữ bản snapshot để bootstrap lại phiên sau reload.
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

  function getPermissions(): string[] {
    return authState.value?.permissions ?? [];
  }

  function hasPermission(permissionCode: string): boolean {
    const permissions = getPermissions();
    if (permissions.length === 0) {
      // Backend hiện phát hành access token kèm permission claims; nhánh này chủ yếu bảo vệ các token cũ hoặc token lỗi parse và tránh chặn nhầm ở lớp client.
      return true;
    }

    return permissions.includes(permissionCode);
  }

  return {
    authState,
    isAuthenticated,
    setAuthState,
    clearAuthState,
    getAccessToken,
    getAuthState,
    getPermissions,
    hasPermission
  };
});

function readAuthState(): AuthState | null {
  try {
    const stored = sessionStorage.getItem(AUTH_STORAGE_KEY);
    if (!stored) {
      return null;
    }

    const parsed = JSON.parse(stored) as Partial<AuthState>;
    if (!parsed.accessToken || !parsed.accessTokenExpiresAt || !parsed.refreshTokenExpiresAt || !parsed.receivedAt) {
      clearLegacyPersistentAuthState();
      return null;
    }

    return {
      accessToken: parsed.accessToken,
      accessTokenExpiresAt: parsed.accessTokenExpiresAt,
      refreshTokenExpiresAt: parsed.refreshTokenExpiresAt,
      receivedAt: parsed.receivedAt,
      permissions: Array.isArray(parsed.permissions)
        ? parsed.permissions.filter((item): item is string => typeof item === 'string')
        : extractPermissions(parsed.accessToken)
    };
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

function extractPermissions(accessToken: string): string[] {
  const payload = readJwtPayload(accessToken);
  if (!payload) {
    return [];
  }

  // Hỗ trợ cả claim lặp `permission`, mảng `permissions`, và chuỗi `scope` để tương thích với nhiều kiểu phát hành JWT.
  const directPermissions = normalizePermissionClaim(payload.permission);
  const listPermissions = normalizePermissionClaim(payload.permissions);
  const scopedPermissions = typeof payload.scope === 'string'
    ? payload.scope.split(' ').map((item) => item.trim()).filter(Boolean)
    : [];

  return Array.from(new Set([...directPermissions, ...listPermissions, ...scopedPermissions]));
}

function readJwtPayload(accessToken: string): Record<string, unknown> | null {
  const parts = accessToken.split('.');
  if (parts.length < 2) {
    return null;
  }

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const normalized = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return JSON.parse(atob(normalized)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function normalizePermissionClaim(claim: unknown): string[] {
  if (typeof claim === 'string') {
    return [claim];
  }

  if (Array.isArray(claim)) {
    return claim.filter((item): item is string => typeof item === 'string');
  }

  return [];
}
