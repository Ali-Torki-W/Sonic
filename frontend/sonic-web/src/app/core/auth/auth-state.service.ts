import { Injectable, computed, signal } from '@angular/core';
import { clearAuth, readAccessToken, writeAuth } from './token-storage';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
    private readonly _accessToken = signal<string | null>(readAccessToken());

    readonly accessToken = this._accessToken.asReadonly();

    readonly isAuthenticated = computed(() => {
        const t = this._accessToken();
        return !!t && t.trim().length > 0;
    });

    setSession(accessToken: string, expiresAtUtcIso: string): void {
        writeAuth(accessToken, expiresAtUtcIso);
        this._accessToken.set(accessToken);
    }

    logout(): void {
        clearAuth();
        this._accessToken.set(null);
    }
}
