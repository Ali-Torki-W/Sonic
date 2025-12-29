import { Injectable, inject } from '@angular/core';
import { tap } from 'rxjs/operators';
import { ApiClient } from '../http/api-client';
import { AuthStateService } from './auth-state.service';
import { LoginRequest } from '../../shared/contracts/auth/login-request';
import { LoginResponse } from '../../shared/contracts/auth/login-response';
import { RegisterRequest } from '../../shared/contracts/auth/register-request';
import { RegisterResponse } from '../../shared/contracts/auth/register-response';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly api = inject(ApiClient);
    private readonly authState = inject(AuthStateService);

    login(req: LoginRequest) {
        return this.api.post<LoginResponse>('/auth/login', req).pipe(
            tap(resp => this.authState.setSession(resp.accessToken, resp.expiresAtUtc))
        );
    }

    register(req: RegisterRequest) {
        return this.api.post<RegisterResponse>('/auth/register', req).pipe(
            // Auto-login after registration
            tap(resp => this.authState.setSession(resp.accessToken, resp.expiresAtUtc))
        );
    }

    logout(): void {
        this.authState.clearSession();
    }
}