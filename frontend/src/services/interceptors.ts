let accessTokenProvider: (() => string | null) | undefined;

export function setAccessTokenProvider(provider: () => string | null) {
  accessTokenProvider = provider;
}

export function getAccessToken(): string | null {
  return accessTokenProvider?.() ?? null;
}
