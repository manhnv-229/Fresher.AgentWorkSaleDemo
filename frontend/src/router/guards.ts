import { getAccessToken } from '../stores/auth';

export function isAuthenticatedRoute() {
  // Guard mức client chỉ kiểm tra token đang có; quyền thực tế vẫn do API xác nhận.
  return getAccessToken() !== null;
}
