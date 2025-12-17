import { PostType } from "./post-type";

export interface CreatePostRequest {
    type: PostType;
    title: string;
    body: string;
    tags: string[];
    externalLink?: string | null;
    campaignGoal?: string | null;
}