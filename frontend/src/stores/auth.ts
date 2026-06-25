import { computed } from 'vue';
import type { AuthState, TokenResponse } from '../api/auth.types';
import { useAuthStore } from './useAuthStore';

export const readonlyAuthState = computed<AuthState | null>(() => useAuthStore().authState);

export function setAuthState(tokens: TokenResponse): AuthState {
  return useAuthStore().setAuthState(tokens);
}

export function clearAuthState(): void {
  useAuthStore().clearAuthState();
}

export function getAccessToken(): string | null {
  return useAuthStore().getAccessToken();
}

export function getAuthState(): AuthState | null {
  return useAuthStore().getAuthState();
}
