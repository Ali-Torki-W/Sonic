import { CanActivateFn } from '@angular/router';

export const authLoggedInGuard: CanActivateFn = (route, state) => {
  return true;
};
