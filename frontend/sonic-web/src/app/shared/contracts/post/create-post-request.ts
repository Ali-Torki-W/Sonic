import { PostType } from "../../enums/post-type";

export interface CreatePostRequest {
    type: PostType;
    title: string;
    body: string;
    tags: string[];
    externalLink?: string | null;
    campaignGoal?: string | null;
}