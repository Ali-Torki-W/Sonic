import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthStateService } from '../auth/auth-state.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authState = inject(AuthStateService);
    const token = authState.accessToken();

    if (!token) {
        return next(req);
    }

    const clonedReq = req.clone({
        setHeaders: {
            Authorization: `Bearer ${token}`
        },
    });

    return next(clonedReq);
};