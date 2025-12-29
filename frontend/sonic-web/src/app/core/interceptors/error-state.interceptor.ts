import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { AuthStateService } from '../auth/auth-state.service';
import { ProblemDetails } from '../http/problem-details';

export const errorStateInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthStateService);
    const router = inject(Router);
    const snack = inject(MatSnackBar);

    return next(req).pipe(
        catchError((err: HttpErrorResponse) => {
            // Because problemDetailsInterceptor ran first, 
            // `err.error` is GUARANTEED to be of type ProblemDetails.
            const problem = err.error as ProblemDetails;

            if (err.status === 401) {
                auth.clearSession();

                // Use the message from the ProblemDetails (which might be custom from backend)
                snack.open(problem.detail || 'Session expired.', 'OK', { duration: 5000 });

                router.navigate(['/account/login'], {
                    queryParams: { returnUrl: router.url }
                });
            }
            else if (err.status === 403) {
                snack.open(problem.detail || 'Access Denied.', 'OK', { duration: 5000 });
            }
            else if (err.status >= 500) {
                snack.open('Server Error: ' + problem.detail, 'OK', { duration: 5000 });
            }

            // Always re-throw so the component can stop its loading spinner
            return throwError(() => err);
        })
    );
};