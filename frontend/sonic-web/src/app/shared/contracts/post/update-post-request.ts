export interface UpdatePostRequest {
    title: string;
    body: string;
    tags: string[];
    externalLink?: string | null;
    campaignGoal?: string | null;
}