import { readonly, ref } from 'vue';
import type { AuthState, TokenResponse } from '../api/auth.types';

const AUTH_STORAGE_KEY = 'demo.auth';

const authState = ref<AuthState | null>(readAuthState());

export const readonlyAuthState = readonly(authState);

export function setAuthState(tokens: TokenResponse): AuthState {
  const nextAuthState: AuthState = {
    accessToken: tokens.accessToken,
    accessTokenExpiresAt: tokens.accessTokenExpiresAt,
    refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
    receivedAt: new Date().toISOString()
  };

  authState.value = nextAuthState;
  sessionStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(nextAuthState));
  return nextAuthState;
}

export function clearAuthState(): void {
  authState.value = null;
  clearLegacyPersistentAuthState();
}

export function getAccessToken(): string | null {
  return authState.value?.accessToken ?? null;
}

export function getAuthState(): AuthState | null {
  return authState.value;
}

function readAuthState(): AuthState | null {
  try {
    const stored = sessionStorage.getItem(AUTH_STORAGE_KEY);
    if (!stored) {
      return null;
    }

    return JSON.parse(stored) as AuthState;
  } catch {
    clearLegacyPersistentAuthState();
    return null;
  }
}

function clearLegacyPersistentAuthState(): void {
  localStorage.removeItem(AUTH_STORAGE_KEY);
  sessionStorage.removeItem(AUTH_STORAGE_KEY);
}
