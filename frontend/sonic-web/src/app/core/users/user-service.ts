import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../http/api-client';
import { PublicProfileResponse } from '../../shared/contracts/user/public-profile-response';

@Injectable({ providedIn: 'root' })
export class UsersService {
    private readonly api = inject(ApiClient);

    getPublicProfile(userId: string): Observable<PublicProfileResponse> {
        const id = (userId ?? '').trim();
        return this.api.get<PublicProfileResponse>(`/users/${id}`);
    }
}
