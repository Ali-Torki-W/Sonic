import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../http/api-client';
import { PostResponse } from '../../shared/contracts/post/post-response';
import { LikeToggleResponse } from '../../shared/contracts/like/like-toggle-response';
import { CreatePostRequest } from '../../shared/contracts/post/create-post-request';
import { UpdatePostRequest } from '../../shared/contracts/post/update-post-request';

@Injectable({ providedIn: 'root' })
export class PostsService {
    private readonly api = inject(ApiClient);

    getById(postId: string): Observable<PostResponse> {
        const id = (postId ?? '').trim();
        return this.api.get<PostResponse>(`/posts/${encodeURIComponent(id)}`);
    }

    create(request: CreatePostRequest): Observable<PostResponse> {
        return this.api.post<PostResponse>(`/posts`, request);
    }

    update(postId: string, request: UpdatePostRequest): Observable<PostResponse> {
        const id = (postId ?? '').trim();
        return this.api.put<PostResponse>(`/posts/${encodeURIComponent(id)}`, request);
    }

    delete(postId: string): Observable<void> {
        const id = (postId ?? '').trim();
        return this.api.delete<void>(`/posts/${encodeURIComponent(id)}`);
    }

    toggleLike(postId: string): Observable<LikeToggleResponse> {
        const id = (postId ?? '').trim();
        return this.api.post<LikeToggleResponse>(`/posts/${encodeURIComponent(id)}/like`, {});
    }

    getLikeStatus(postId: string): Observable<LikeToggleResponse> {
        return this.api.get<LikeToggleResponse>(`/posts/${encodeURIComponent(postId)}/like`);
    }
}
