import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

import { useAuthStore } from '../../src/stores/useAuthStore';

describe('useAuthStore', () => {
  beforeEach(() => {
    vi.setSystemTime(new Date('2026-07-09T00:00:00Z'));
  });

  it('should persist auth state and extract permissions from jwt claims', () => {
    const store = useAuthStore();

    const accessToken = createJwt({
      permission: 'agent.read',
      permissions: ['agent.write', 'agent.read'],
      scope: 'agent.delete agent.write'
    });

    const authState = store.setAuthState({
      accessToken,
      accessTokenExpiresAt: '2026-07-10T00:00:00Z',
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    });

    expect(store.isAuthenticated).toBe(true);
    expect(authState.permissions).toEqual([
      'agent.read',
      'agent.write',
      'agent.delete'
    ]);
    expect(store.getAccessToken()).toBe(accessToken);
    expect(JSON.parse(window.sessionStorage.getItem('demo.auth') ?? '{}')).toMatchObject({
      accessToken,
      accessTokenExpiresAt: '2026-07-10T00:00:00Z',
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    });
  });

  it('should clear auth state from memory and storage', () => {
    const store = useAuthStore();
    window.localStorage.setItem('demo.auth', 'legacy');
    window.sessionStorage.setItem('demo.auth', 'legacy');

    store.setAuthState({
      accessToken: createJwt({ permission: 'agent.read' }),
      accessTokenExpiresAt: '2026-07-10T00:00:00Z',
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    });
    store.clearAuthState();

    expect(store.authState).toBeNull();
    expect(store.isAuthenticated).toBe(false);
    expect(window.localStorage.getItem('demo.auth')).toBeNull();
    expect(window.sessionStorage.getItem('demo.auth')).toBeNull();
  });

  it('should default hasPermission to true when token has no permissions', () => {
    const store = useAuthStore();

    store.setAuthState({
      accessToken: createJwt({}),
      accessTokenExpiresAt: '2026-07-10T00:00:00Z',
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    });

    expect(store.getPermissions()).toEqual([]);
    expect(store.hasPermission('agent.read')).toBe(true);
  });

  it('should return false when permissions exist but target permission is missing', () => {
    const store = useAuthStore();

    store.setAuthState({
      accessToken: createJwt({ permissions: ['agent.read'] }),
      accessTokenExpiresAt: '2026-07-10T00:00:00Z',
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    });

    expect(store.hasPermission('agent.write')).toBe(false);
    expect(store.hasPermission('agent.read')).toBe(true);
  });

  it('should ignore invalid persisted auth state during bootstrap', () => {
    window.sessionStorage.setItem('demo.auth', '{invalid-json');

    setActivePinia(createPinia());
    const store = useAuthStore();

    expect(store.authState).toBeNull();
    expect(window.sessionStorage.getItem('demo.auth')).toBeNull();
  });

  function createJwt(payload: Record<string, unknown>) {
    const header = base64UrlEncode({ alg: 'none', typ: 'JWT' });
    const body = base64UrlEncode(payload);
    return `${header}.${body}.signature`;
  }

  function base64UrlEncode(value: Record<string, unknown>) {
    return btoa(JSON.stringify(value))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/g, '');
  }
});
