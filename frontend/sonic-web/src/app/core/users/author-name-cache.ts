import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay } from 'rxjs/operators';
import { UsersService } from './user-service';


@Injectable({ providedIn: 'root' })
export class AuthorDisplayNameCache {
    private readonly users = inject(UsersService);

    private readonly cache = new Map<string, Observable<string | null>>();

    getDisplayName(userId: string): Observable<string | null> {
        const id = (userId ?? '').trim();
        if (!id) return of(null);

        const existing = this.cache.get(id);
        if (existing) return existing;

        const req$ = this.users.getPublicProfile(id).pipe(
            map(p => {
                const name = (p?.displayName ?? '').trim();
                return name.length > 0 ? name : null;
            }),
            catchError(() => of(null)),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        this.cache.set(id, req$);
        return req$;
    }

    seed(userId: string, displayName: string): void {
        const id = (userId ?? '').trim();
        const name = (displayName ?? '').trim();
        if (!id || !name) return;

        this.cache.set(id, of(name));
    }
}
