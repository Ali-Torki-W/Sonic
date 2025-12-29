import { Injectable, computed, inject, signal, DestroyRef } from '@angular/core'; // Added DestroyRef
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { UsersService } from './user-service';
import { AuthStateService } from '../auth/auth-state.service';
import { GetCurrentUserResponse } from '../../shared/contracts/user/get-current-user-response';

@Injectable({ providedIn: 'root' })
export class CurrentUserStore {
    private readonly users = inject(UsersService);
    private readonly auth = inject(AuthStateService);
    private readonly destroyRef = inject(DestroyRef); // ✅ Inject it here

    private readonly meSig = signal<GetCurrentUserResponse | null>(null);
    private readonly loadingSig = signal(false);

    readonly me = this.meSig.asReadonly();
    readonly loading = this.loadingSig.asReadonly();
    readonly isReady = computed(() => !this.loadingSig() && this.meSig() !== null);

    readonly displayName = computed(() => this.meSig()?.displayName ?? null);
    readonly avatarUrl = computed(() => this.meSig()?.avatarUrl ?? null);
    readonly role = computed(() => this.meSig()?.role ?? null);
    readonly id = computed(() => this.meSig()?.id ?? null);

    // ✅ No arguments needed anymore
    loadIfNeeded(): void {
        // If not authenticated, clear data
        if (!this.auth.isAuthenticated()) {
            this.clear();
            return;
        }

        // If already loaded or currently loading, skip
        if (this.meSig() || this.loadingSig()) return;

        this.loadingSig.set(true);

        this.users
            .getMe()
            .pipe(takeUntilDestroyed(this.destroyRef)) // ✅ Use local reference
            .subscribe({
                next: (me) => {
                    this.meSig.set(me);
                    this.loadingSig.set(false);
                },
                error: () => {
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