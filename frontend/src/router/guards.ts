import { loadAuthState } from '../features/auth/store';

export function isAuthenticatedRoute() {
  return loadAuthState() !== null;
}
