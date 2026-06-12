import type { StoredAuthState, TokenResponse } from './types/auth.types';

const AUTH_STORAGE_KEY = 'demo.auth';

export function saveAuthState(tokens: TokenResponse): StoredAuthState {
  const authState: StoredAuthState = {
    ...tokens,
    savedAt: new Date().toISOString()
  };

  localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(authState));
  return authState;
}

export function loadAuthState(): StoredAuthState | null {
  const rawValue = localStorage.getItem(AUTH_STORAGE_KEY);
  if (!rawValue) {
    return null;
  }

  try {
    return JSON.parse(rawValue) as StoredAuthState;
  } catch {
    clearAuthState();
    return null;
  }
}

export function clearAuthState(): void {
  localStorage.removeItem(AUTH_STORAGE_KEY);
}
