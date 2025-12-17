export const ACCESS_TOKEN_KEY = 'sonic.accessToken';
export const EXPIRES_AT_KEY = 'sonic.expiresAtUtc';

export function readAccessToken(): string | null {
    const t = localStorage.getItem(ACCESS_TOKEN_KEY);
    return t && t.trim().length > 0 ? t : null;
}

export function writeAuth(accessToken: string, expiresAtUtcIso: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(EXPIRES_AT_KEY, expiresAtUtcIso);
}

export function clearAuth(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
}
