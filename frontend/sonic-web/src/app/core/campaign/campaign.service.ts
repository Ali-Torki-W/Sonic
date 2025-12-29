import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiClient } from '../http/api-client';
import { PagedResult } from '../../shared/contracts/common/paged-result';
import { PostResponse } from '../../shared/contracts/post/post-response';
import { CampaignJoinResponse } from '../../shared/contracts/campaign/campaign-join-response';

export interface CampaignsQuery {
    page: number;
    pageSize: number;
    tags?: readonly string[] | null;
    q?: string | null;
    featured?: boolean | null;
}

@Injectable({ providedIn: 'root' })
export class CampaignsService {
    private readonly api = inject(ApiClient);

    getCampaigns(query: CampaignsQuery): Observable<PagedResult<PostResponse>> {
        let params = new HttpParams()
            .set('page', String(query.page))
            .set('pageSize', String(query.pageSize));

        for (const t of (query.tags ?? [])) {
            const tag = t?.trim();
            if (tag) params = params.append('tag', tag);
        }

        const q = query.q?.trim();
        if (q) params = params.set('q', q);

        if (query.featured === true) {
            params = params.set('featured', 'true');
        }

        return this.api.get<PagedResult<PostResponse>>('/campaigns', params);
    }

    join(postId: string): Observable<CampaignJoinResponse> {
        const id = (postId ?? '').trim();
        return this.api.post<CampaignJoinResponse>(`/campaigns/${encodeURIComponent(id)}/join`, {});
    }

    getJoinStatus(postId: string): Observable<CampaignJoinResponse> {
        const id = (postId ?? '').trim();
        return this.api.get<CampaignJoinResponse>(`/campaigns/${encodeURIComponent(id)}/join`);
    }
}
