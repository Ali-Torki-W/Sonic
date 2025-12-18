import { Injectable, inject } from '@angular/core';
import { ApiClient } from '../http/api-client';
import { LoginRequest } from '../../shared/contracts/auth/login-request';
import { LoginResponse } from '../../shared/contracts/auth/login-response';
import { RegisterRequest } from '../../shared/contracts/auth/register-request';
import { RegisterResponse } from '../../shared/contracts/auth/register-response';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly api = inject(ApiClient);

    login(req: LoginRequest) {
        return this.api.post<LoginResponse>('/auth/login', req);
    }

    register(req: RegisterRequest) {
        return this.api.post<RegisterResponse>('/auth/register', req);
    }
}
