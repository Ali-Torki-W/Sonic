import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthStateService } from '../auth/auth-state.service';

export const authLoggedInGuard: CanActivateFn = () => {
  const router = inject(Router);
  const auth = inject(AuthStateService);

  if (!auth.isAuthenticated()) return true;

  router.navigate(['/feed']);
  return false;
};
