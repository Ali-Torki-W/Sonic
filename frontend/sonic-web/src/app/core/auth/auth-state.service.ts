import { Injectable, computed, signal } from '@angular/core';

const ACCESS_TOKEN_KEY = 'sonic.accessToken';
const EXPIRES_AT_UTC_KEY = 'sonic.expiresAtUtc';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
    private readonly accessTokenSig = signal<string | null>(null);
    private readonly expiresAtUtcSig = signal<string | null>(null);

    readonly accessToken = this.accessTokenSig.asReadonly();
    readonly expiresAtUtc = this.expiresAtUtcSig.asReadonly();

    readonly isAuthenticated = computed(() => {
        const token = this.accessTokenSig();
        if (!token) return false;

        const expiresAt = this.expiresAtUtcSig();
        if (!expiresAt) return false;

        const ms = Date.parse(expiresAt);
        if (Number.isNaN(ms)) return false;

        return Date.now() < ms;
    });

    constructor() {
        const token = localStorage.getItem(ACCESS_TOKEN_KEY);
        const expiresAt = localStorage.getItem(EXPIRES_AT_UTC_KEY);

        this.accessTokenSig.set(token);
        this.expiresAtUtcSig.set(expiresAt);
    }

    setSession(accessToken: string, expiresAtUtc: string): void {
        localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
        localStorage.setItem(EXPIRES_AT_UTC_KEY, expiresAtUtc);

        this.accessTokenSig.set(accessToken);
        this.expiresAtUtcSig.set(expiresAtUtc);
    }

    clearSession(): void {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(EXPIRES_AT_UTC_KEY);

        this.accessTokenSig.set(null);
        this.expiresAtUtcSig.set(null);
    }

    getAccessToken(): string | null {
        return this.accessTokenSig();
    }
}
