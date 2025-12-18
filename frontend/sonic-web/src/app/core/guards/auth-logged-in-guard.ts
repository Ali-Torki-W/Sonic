import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStateService } from '../auth/auth-state.service';

export const authLoggedInGuard: CanActivateFn = (_route, _state) => {
  const authState = inject(AuthStateService);
  const router = inject(Router);

  if (!authState.isAuthenticated()) return true;

  // Already logged in => keep them out of login/register pages.
  return router.createUrlTree(['/dashboard']);
};
