import { Injectable, computed, signal, inject, PLATFORM_ID, OnDestroy } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const ACCESS_TOKEN_KEY = 'sonic.accessToken';
const EXPIRES_AT_UTC_KEY = 'sonic.expiresAtUtc';

@Injectable({ providedIn: 'root' })
export class AuthStateService implements OnDestroy {
    private readonly platformId = inject(PLATFORM_ID);
    private readonly isBrowser = isPlatformBrowser(this.platformId);

    // Signals
    private readonly accessTokenSig = signal<string | null>(null);
    private readonly expiresAtUtcSig = signal<string | null>(null);

    // Read-only Public API
    readonly accessToken = this.accessTokenSig.asReadonly();
    readonly expiresAtUtc = this.expiresAtUtcSig.asReadonly();

    readonly isAuthenticated = computed(() => {
        const token = this.accessTokenSig();
        const expiresAt = this.expiresAtUtcSig();

        if (!token || !expiresAt) return false;

        const ms = Date.parse(expiresAt);
        return !Number.isNaN(ms) && Date.now() < ms;
    });

    constructor() {
        if (this.isBrowser) {
            this.initializeFromStorage();
            window.addEventListener('storage', this.handleStorageEvent);
        }
    }

    ngOnDestroy(): void {
        if (this.isBrowser) {
            window.removeEventListener('storage', this.handleStorageEvent);
        }
    }

    setSession(accessToken: string, expiresAtUtc: string): void {
        this.updateState(accessToken, expiresAtUtc);

        if (this.isBrowser) {
            localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
            localStorage.setItem(EXPIRES_AT_UTC_KEY, expiresAtUtc);
        }
    }

    clearSession(): void {
        this.updateState(null, null);

        if (this.isBrowser) {
            localStorage.removeItem(ACCESS_TOKEN_KEY);
            localStorage.removeItem(EXPIRES_AT_UTC_KEY);
        }
    }

    private readonly handleStorageEvent = (event: StorageEvent) => {
        if (event.key === ACCESS_TOKEN_KEY || event.key === EXPIRES_AT_UTC_KEY) {
            this.initializeFromStorage();
        }
    };

    private initializeFromStorage(): void {
        const token = localStorage.getItem(ACCESS_TOKEN_KEY);
        const expiresAt = localStorage.getItem(EXPIRES_AT_UTC_KEY);
        this.updateState(token, expiresAt);
    }

    private updateState(token: string | null, expiresAt: string | null): void {
        // Ensure we don't store empty strings or whitespace as valid tokens
        const validToken = token && token.trim().length > 0 ? token : null;
        const validDate = expiresAt && expiresAt.trim().length > 0 ? expiresAt : null;

        this.accessTokenSig.set(validToken);
        this.expiresAtUtcSig.set(validDate);
    }
}