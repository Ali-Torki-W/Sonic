export interface PublicProfileResponse {
    id: string;
    displayName: string;
    bio?: string | null;
    jobRole: string;
    avatarUrl?: string | null;
}