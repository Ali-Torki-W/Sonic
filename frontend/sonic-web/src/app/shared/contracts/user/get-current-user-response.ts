export interface GetCurrentUserResponse {
    id: string;
    email: string;
    displayName: string;
    bio?: string | null;
    jobRole?: string | null;
    interests: string[];
    avatarUrl?: string | null;
    role: string;
    createdAt: string;
    updatedAt: string;
}