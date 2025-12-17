export interface LikeToggleResponse {
    postId: string;
    likeCount: number; // backend long
    liked: boolean;
}