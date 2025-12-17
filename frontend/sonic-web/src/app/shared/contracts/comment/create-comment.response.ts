export interface CommentResponse {
    id: string;
    postId: string;
    authorId: string;
    authorDisplayName?: string | null;
    body: string;
    createdAt: string;
    updatedAt?: string | null;
}