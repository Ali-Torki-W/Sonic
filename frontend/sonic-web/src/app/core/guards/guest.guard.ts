import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthStateService } from '../auth/auth-state.service';

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthStateService);
  const router = inject(Router);
  const snack = inject(MatSnackBar);

  if (!auth.isAuthenticated()) {
    return true;
  }

  snack.open('You are already logged in.', 'OK', { duration: 3000 });

  return router.createUrlTree(['/feed']);
};