export interface LoginResponse {
    userId: string;
    email: string;
    displayName: string;
    role: string;
    accessToken: string;
    expiresAtUtc: string;
}