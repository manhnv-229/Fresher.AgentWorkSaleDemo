let accessTokenProvider: (() => string | null) | undefined;

// Đăng ký hàm đọc access token runtime để lớp HTTP không phụ thuộc trực tiếp vào Pinia.
export function setAccessTokenProvider(provider: () => string | null) {
  accessTokenProvider = provider;
}

// Trả access token hiện tại nếu provider đã được cấu hình khi app khởi động.
export function getAccessToken(): string | null {
  return accessTokenProvider?.() ?? null;
}
