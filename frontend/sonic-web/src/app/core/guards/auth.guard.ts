import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthStateService } from '../auth/auth-state.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  const snack = inject(MatSnackBar);
  const auth = inject(AuthStateService);

  if (auth.isAuthenticated()) {
    return true;
  }

  snack.open('You must be logged in to view this page.', 'OK', { duration: 4000 });

  return router.createUrlTree(['/account/login'], {
    queryParams: { returnUrl: state.url }
  });
};