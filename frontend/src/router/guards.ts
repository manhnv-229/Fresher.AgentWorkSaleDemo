import { useAuthStore } from '../stores/useAuthStore';

export function isAuthenticatedRoute() {
  // Guard mức client chỉ kiểm tra token đang có; quyền thực tế vẫn do API xác nhận.
  return useAuthStore().getAccessToken() !== null;
}
