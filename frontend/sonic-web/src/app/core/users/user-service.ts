import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../http/api-client';
import { GetCurrentUserResponse } from '../../shared/contracts/user/get-current-user-response';
import { PublicProfileResponse } from '../../shared/contracts/user/public-profile-response';

@Injectable({ providedIn: 'root' })
export class UsersService {
    private readonly api = inject(ApiClient);

    getMe(): Observable<GetCurrentUserResponse> {
        return this.api.get<GetCurrentUserResponse>('/users/me');
    }

    getPublicProfile(userId: string): Observable<PublicProfileResponse> {
        const id = (userId ?? '').trim();
        return this.api.get<PublicProfileResponse>(`/users/${encodeURIComponent(id)}`);
    }
}
