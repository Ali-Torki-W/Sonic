import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiClient } from '../http/api-client';
import { PagedResult } from '../../shared/contracts/common/paged-result';
import { CommentResponse } from '../../shared/contracts/comment/create-comment.response';
import { CreateCommentRequest } from '../../shared/contracts/comment/create-comment-request';

@Injectable({ providedIn: 'root' })
export class CommentsService {
    private readonly api = inject(ApiClient);

    getForPost(postId: string, page: number, pageSize: number): Observable<PagedResult<CommentResponse>> {
        const id = (postId ?? '').trim();

        const params = new HttpParams()
            .set('page', String(page))
            .set('pageSize', String(pageSize));

        return this.api.get<PagedResult<CommentResponse>>(
            `/posts/${encodeURIComponent(id)}/comments`,
            params
        );
    }

    create(postId: string, req: CreateCommentRequest): Observable<CommentResponse> {
        const id = (postId ?? '').trim();
        return this.api.post<CommentResponse>(`/posts/${encodeURIComponent(id)}/comments`, req);
    }

    delete(commentId: string): Observable<void> {
        const id = (commentId ?? '').trim();
        return this.api.delete<void>(`/comments/${encodeURIComponent(id)}`);
    }
}
