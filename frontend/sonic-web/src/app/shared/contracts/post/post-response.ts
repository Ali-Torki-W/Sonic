import { PostType } from "./post-type";

export interface PostResponse {
    id: string;
    type: PostType;
    title: string;
    body: string;
    tags: string[];
    externalLink?: string | null;
    authorId: string;
    createdAt: string;
    updatedAt: string;
    isFeatured: boolean;
    likeCount: number; // backend long
    campaignGoal?: string | null;
    participantsCount: number; // backend long
}