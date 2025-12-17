import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthStateService } from '../auth/auth-state.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const auth = inject(AuthStateService);
    const token = auth.accessToken();

    if (!token) return next(req);

    return next(
        req.clone({
            setHeaders: { Authorization: `Bearer ${token}` },
        }),
    );
};
