export interface UpdateProfileRequest {
    displayName: string;
    bio?: string | null;
    jobRole?: string | null;
    interests?: string[] | null;
    avatarUrl?: string | null;
}