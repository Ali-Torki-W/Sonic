import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay } from 'rxjs/operators';
import { UsersService } from './user-service';


@Injectable({ providedIn: 'root' })
export class AuthorNameCache {
    private readonly users = inject(UsersService);

    private readonly cache = new Map<string, string>();
    private readonly inflight = new Map<string, Observable<string>>();

    getDisplayName(userId: string): Observable<string> {
        const id = (userId ?? '').trim();
        if (!id) return of('Unknown');

        const cached = this.cache.get(id);
        if (cached) return of(cached);

        const existing = this.inflight.get(id);
        if (existing) return existing;

        const req$ = this.users.getPublicProfile(id).pipe(
            map(p => {
                const name = (p?.displayName ?? '').trim();
                const finalName = name || this.shortId(id);
                this.cache.set(id, finalName);
                return finalName;
            }),
            catchError(() => of(this.shortId(id))),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.inflight.set(id, req$);
        req$.subscribe({ complete: () => this.inflight.delete(id) });

        return req$;
    }

    seed(userId: string, displayName: string): void {
        const id = (userId ?? '').trim();
        const name = (displayName ?? '').trim();
        if (!id || !name) return;
        this.cache.set(id, name);
    }

    private shortId(id: string): string {
        return id.length > 10 ? `${id.slice(0, 6)}…${id.slice(-4)}` : id;
    }
}
