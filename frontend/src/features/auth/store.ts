import { readonly, ref } from 'vue';
import type { AuthState, TokenResponse } from './types/auth.types';

const AUTH_STORAGE_KEY = 'demo.auth';

const authState = ref<AuthState | null>(null);

export const readonlyAuthState = readonly(authState);

export function setAuthState(tokens: TokenResponse): AuthState {
  clearLegacyPersistentAuthState();

  const nextAuthState: AuthState = {
    accessToken: tokens.accessToken,
    accessTokenExpiresAt: tokens.accessTokenExpiresAt,
    refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
    receivedAt: new Date().toISOString()
  };

  authState.value = nextAuthState;
  return nextAuthState;
}

export function clearAuthState(): void {
  authState.value = null;
  clearLegacyPersistentAuthState();
}

export function getAccessToken(): string | null {
  return authState.value?.accessToken ?? null;
}

function clearLegacyPersistentAuthState(): void {
  localStorage.removeItem(AUTH_STORAGE_KEY);
  sessionStorage.removeItem(AUTH_STORAGE_KEY);
}
