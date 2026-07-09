import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

import type { TokenResponse } from '../../src/api/auth.types';

describe('useAuth', () => {
  const routerReplace = vi.fn();
  const loginMock = vi.fn<(...args: unknown[]) => Promise<TokenResponse>>();
  const refreshMock = vi.fn<() => Promise<TokenResponse>>();
  const changePasswordMock = vi.fn<() => Promise<void>>();
  const logoutMock = vi.fn<() => Promise<void>>();

  beforeEach(() => {
    vi.resetModules();
    setActivePinia(createPinia());
    routerReplace.mockReset();
    loginMock.mockReset();
    refreshMock.mockReset();
    changePasswordMock.mockReset();
    logoutMock.mockReset();
  });

  it('should login and update auth state', async () => {
    const tokens = createTokenResponse('header.payload.signature');
    loginMock.mockResolvedValue(tokens);

    const { useAuth } = await loadUseAuth();
    const { useAuthStore } = await import('../../src/stores/useAuthStore');
    const auth = useAuth();

    await auth.login('demo@example.com', 'Password123!');

    const store = useAuthStore();
    expect(loginMock).toHaveBeenCalledWith({ email: 'demo@example.com', password: 'Password123!' });
    expect(store.getAuthState()).not.toBeNull();
    expect(auth.isAuthenticated.value).toBe(true);
  });

  it('should clear auth state and redirect to login after changing password', async () => {
    const tokens = createTokenResponse('very.long.jwt.token.value');
    changePasswordMock.mockResolvedValue(undefined);

    const { useAuth } = await loadUseAuth('/settings/password');
    const { useAuthStore } = await import('../../src/stores/useAuthStore');
    const store = useAuthStore();
    store.setAuthState(tokens);

    const auth = useAuth();
    await auth.changePassword('Current123!', 'New123!');

    expect(changePasswordMock).toHaveBeenCalledWith({
      currentPassword: 'Current123!',
      newPassword: 'New123!'
    });
    expect(store.getAuthState()).toBeNull();
    expect(routerReplace).toHaveBeenCalledWith({
      name: 'login',
      query: { reason: 'password-changed', redirect: '/settings/password' }
    });
  });

  it('should not refresh during initialize when current token is still valid', async () => {
    const tokens = createTokenResponse('valid.token.signature', '2099-07-10T00:00:00Z');

    const { useAuth } = await loadUseAuth();
    const { useAuthStore } = await import('../../src/stores/useAuthStore');
    const store = useAuthStore();
    store.setAuthState(tokens);

    const auth = useAuth();
    await auth.initializeAuth();

    expect(refreshMock).not.toHaveBeenCalled();
    expect(auth.isInitializing.value).toBe(false);
    expect(store.getAuthState()).not.toBeNull();
  });

  it('should clear auth state when startup refresh fails', async () => {
    refreshMock.mockRejectedValue(new Error('refresh failed'));

    const { useAuth } = await loadUseAuth();
    const { useAuthStore } = await import('../../src/stores/useAuthStore');
    const store = useAuthStore();
    store.setAuthState(createTokenResponse('stale.token.signature', '2000-01-01T00:00:00Z'));

    const auth = useAuth();
    await auth.initializeAuth();

    expect(refreshMock).toHaveBeenCalledTimes(1);
    expect(store.getAuthState()).toBeNull();
    expect(auth.isInitializing.value).toBe(false);
  });

  it('should clear auth state even when logout request fails', async () => {
    logoutMock.mockRejectedValue(new Error('network'));

    const { useAuth } = await loadUseAuth();
    const { useAuthStore } = await import('../../src/stores/useAuthStore');
    const store = useAuthStore();
    store.setAuthState(createTokenResponse('logout.token.signature'));

    const auth = useAuth();
    await expect(auth.logout()).rejects.toThrow('network');
    expect(store.getAuthState()).toBeNull();
  });

  async function loadUseAuth(currentPath = '/dashboard') {
    vi.doMock('vue-router', () => ({
      useRouter: () => ({
        currentRoute: { value: { fullPath: currentPath } },
        replace: routerReplace
      })
    }));

    vi.doMock('../../src/api', () => ({
      login: loginMock,
      refreshAccessToken: refreshMock,
      changePassword: changePasswordMock,
      logout: logoutMock
    }));

    return import('../../src/composables/useAuth');
  }

  function createTokenResponse(
    accessToken: string,
    accessTokenExpiresAt = '2026-07-10T00:00:00Z'
  ): TokenResponse {
    return {
      accessToken,
      accessTokenExpiresAt,
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    };
  }
});
