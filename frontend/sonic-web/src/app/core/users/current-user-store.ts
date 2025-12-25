import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UsersService } from './user-service';
import { AuthStateService } from '../auth/auth-state.service';
import { GetCurrentUserResponse } from '../../shared/contracts/user/get-current-user-response';

@Injectable({ providedIn: 'root' })
export class CurrentUserStore {
    private readonly users = inject(UsersService);
    private readonly auth = inject(AuthStateService);

    private readonly meSig = signal<GetCurrentUserResponse | null>(null);
    private readonly loadingSig = signal(false);

    readonly me = this.meSig.asReadonly();
    readonly loading = this.loadingSig.asReadonly();

    readonly isReady = computed(() => !this.loadingSig() && this.meSig() !== null);

    readonly displayName = computed(() => this.meSig()?.displayName ?? null);
    readonly avatarUrl = computed(() => this.meSig()?.avatarUrl ?? null);
    readonly role = computed(() => this.meSig()?.role ?? null);
    readonly id = computed(() => this.meSig()?.id ?? null);

    loadIfNeeded(destroyRef: any): void {
        // Don’t call /users/me if not authenticated
        if (!this.auth.isAuthenticated()) {
            this.meSig.set(null);
            this.loadingSig.set(false);
            return;
        }

        // Already loaded
        if (this.meSig()) return;

        this.loadingSig.set(true);

        this.users
            .getMe()
            .pipe(takeUntilDestroyed(destroyRef))
            .subscribe({
                next: (me) => {
                    this.meSig.set(me);
                    this.loadingSig.set(false);
                },
                error: () => {
                    // Do not throw; leave page logic to decide what to do
                    this.meSig.set(null);
                    this.loadingSig.set(false);
                },
            });
    }

    set(me: GetCurrentUserResponse): void {
        this.meSig.set(me);
    }

    clear(): void {
        this.meSig.set(null);
        this.loadingSig.set(false);
    }
}
