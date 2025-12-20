import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';

import { ApiClient } from '../http/api-client';
import { PagedResult } from '../../shared/contracts/common/paged-result';
import { PostResponse } from '../../shared/contracts/post/post-response';
import { PostType } from '../../shared/contracts/post/post-type';

export type PostsFeedQuery = {
    page: number;
    pageSize: number;
    type?: PostType | null;
    tags?: string[] | null;
    q?: string | null;
    featured?: boolean | null;
};

@Injectable({ providedIn: 'root' })
export class PostsService {
    private readonly api = inject(ApiClient);

    getFeed(query: PostsFeedQuery) {
        let params = new HttpParams()
            .set('page', String(query.page))
            .set('pageSize', String(query.pageSize));

        if (query.type) params = params.set('type', query.type as unknown as string);

        if (query.tags && query.tags.length > 0) {
            for (const t of query.tags) {
                const tag = t.trim();
                if (tag) params = params.append('tag', tag); // repeat key: tag=AI&tag=LLM
            }
        }

        if (query.q && query.q.trim()) params = params.set('q', query.q.trim());

        if (query.featured !== null && query.featured !== undefined) {
            params = params.set('featured', String(query.featured));
        }

        return this.api.get<PagedResult<PostResponse>>('/posts', params);
    }
}
