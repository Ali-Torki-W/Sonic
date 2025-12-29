import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiClient } from '../../../core/http/api-client';
import { PagedResult } from '../../../shared/contracts/common/paged-result';
import { PostResponse } from '../../../shared/contracts/post/post-response';
import { PostType } from '../../../shared/contracts/post/post-type';

export interface FeedQuery {
    page: number;
    pageSize: number;
    type?: PostType | null;
    tags?: readonly string[];
    q?: string | null;
    featured?: boolean | null;
}

@Injectable({ providedIn: 'root' })
export class FeedService {
    private readonly api = inject(ApiClient);

    getFeed(query: FeedQuery): Observable<PagedResult<PostResponse>> {
        let params = new HttpParams()
            .set('page', query.page)
            .set('pageSize', query.pageSize);

        if (query.type) {
            params = params.set('type', query.type);
        }

        query.tags?.forEach(tag => {
            if (tag.trim()) params = params.append('tag', tag.trim());
        });

        if (query.q?.trim()) {
            params = params.set('q', query.q.trim());
        }

        if (query.featured) {
            params = params.set('featured', 'true');
        }

        return this.api.get<PagedResult<PostResponse>>('/posts', params);
    }
}