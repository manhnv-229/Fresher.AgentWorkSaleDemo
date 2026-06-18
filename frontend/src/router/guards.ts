import { getAccessToken } from '../stores/auth';

export function isAuthenticatedRoute() {
  return getAccessToken() !== null;
}
