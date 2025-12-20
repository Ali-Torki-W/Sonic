import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../http/api-client';
import { PostResponse } from '../../shared/contracts/post/post-response';
import { LikeToggleResponse } from '../../shared/contracts/like/like-toggle-response';

@Injectable({ providedIn: 'root' })
export class PostsService {
    private readonly api = inject(ApiClient);

    getById(postId: string): Observable<PostResponse> {
        const id = (postId ?? '').trim();
        return this.api.get<PostResponse>(`/posts/${encodeURIComponent(id)}`);
    }

    toggleLike(postId: string): Observable<LikeToggleResponse> {
        const id = (postId ?? '').trim();
        return this.api.post<LikeToggleResponse>(`/posts/${encodeURIComponent(id)}/like`, {});
    }
}
