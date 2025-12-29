import { PostType } from "./post-type";

export interface PostResponse {
    readonly id: string;
    readonly type: PostType;
    readonly title: string;
    readonly body: string;
    readonly tags: readonly string[];

    readonly authorId: string;

    // ✅ NEW FIELD, 
    readonly authorDisplayName: string;

    readonly createdAt: string;
    readonly updatedAt: string;
    readonly isFeatured: boolean;

    readonly likeCount: number;
    readonly participantsCount: number;

    readonly externalLink?: string | null;
    readonly campaignGoal?: string | null;
}