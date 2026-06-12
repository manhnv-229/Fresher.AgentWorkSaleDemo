import { computed, ref } from 'vue';
import { login as loginRequest } from '../api';
import { clearAuthState, loadAuthState, saveAuthState } from '../store';
import type { StoredAuthState } from '../types/auth.types';

const authState = ref<StoredAuthState | null>(loadAuthState());

export function useAuth() {
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
    authState.value = saveAuthState(tokens);
  }

  function logout() {
    clearAuthState();
    authState.value = null;
  }

  return {
    authState,
    isAuthenticated,
    accessTokenPreview,
    login,
    logout
  };
}
