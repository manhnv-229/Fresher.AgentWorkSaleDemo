import { describe, expect, it } from 'vitest';

import { useAuthStore } from '../../src/stores/useAuthStore';
import { canAccessPermissions, isAuthenticatedRoute } from '../../src/router/guards';

describe('router guards', () => {
  it('should detect whether the current route is authenticated from access token presence', () => {
    const store = useAuthStore();

    expect(isAuthenticatedRoute()).toBe(false);

    store.setAuthState({
      accessToken: 'header.payload.signature',
      accessTokenExpiresAt: '2026-07-10T00:00:00Z',
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    });

    expect(isAuthenticatedRoute()).toBe(true);
  });

  it('should allow routes without required permissions', () => {
    expect(canAccessPermissions(undefined)).toBe(true);
    expect(canAccessPermissions([])).toBe(true);
  });

  it('should require every declared permission on protected routes', () => {
    const store = useAuthStore();
    store.setAuthState({
      accessToken: createJwt({ permissions: ['agent.view', 'auditlog.view'] }),
      accessTokenExpiresAt: '2026-07-10T00:00:00Z',
      refreshTokenExpiresAt: '2026-08-10T00:00:00Z'
    });

    expect(canAccessPermissions(['agent.view'])).toBe(true);
    expect(canAccessPermissions(['agent.view', 'auditlog.view'])).toBe(true);
    expect(canAccessPermissions(['agent.view', 'user.view'])).toBe(false);
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
