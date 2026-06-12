import { getAccessToken } from '../features/auth/store';

export function isAuthenticatedRoute() {
  return getAccessToken() !== null;
}
