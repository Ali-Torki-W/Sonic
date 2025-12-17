import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthStateService } from '../auth/auth-state.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  const auth = inject(AuthStateService);

  if (auth.isAuthenticated()) return true;

  router.navigate(['/account/login'], { queryParams: { returnUrl: state.url } });
  return false;
};
