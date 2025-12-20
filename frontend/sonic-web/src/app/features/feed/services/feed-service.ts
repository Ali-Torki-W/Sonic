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
    tags?: readonly string[] | null; // repeat key: tag=AI&tag=LLM
    q?: string | null;               // maps to ?q=
    featured?: boolean | null;        // maps to ?featured=
}

@Injectable({ providedIn: 'root' })
export class FeedService {
    private readonly api = inject(ApiClient);

    getFeed(query: FeedQuery): Observable<PagedResult<PostResponse>> {
        let params = new HttpParams()
            .set('page', String(query.page))
            .set('pageSize', String(query.pageSize));

        if (query.type) {
            params = params.set('type', query.type);
        }

        for (const t of (query.tags ?? [])) {
            const tag = (t ?? '').trim();
            if (tag) params = params.append('tag', tag);
        }

        const q = (query.q ?? '').trim();
        if (q) params = params.set('q', q);

        if (query.featured === true) {
            params = params.set('featured', 'true');
        }
        // featured null/false => omit

        return this.api.get<PagedResult<PostResponse>>('/posts', params);
    }
}
