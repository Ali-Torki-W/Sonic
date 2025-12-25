import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs/operators';

import { UsersService } from '../../../../core/users/user-service';
import { CurrentUserStore } from '../../../../core/users/current-user-store';
import { AuthStateService } from '../../../../core/auth/auth-state.service';

import { GetCurrentUserResponse } from '../../../../shared/contracts/user/get-current-user-response';
import { UpdateProfileRequest } from '../../../../shared/contracts/user/update-profile-request';

type ApiProblem = {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
};

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  private readonly users = inject(UsersService);
  private readonly currentUser = inject(CurrentUserStore);
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  // ---- UI state
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly error = signal<string | null>(null);
  readonly errorCode = signal<string | null>(null);

  readonly submitted = signal(false);

  // ---- interests (one-by-one)
  readonly interests = signal<readonly string[]>([]);
  readonly interestDraft = signal('');

  // ---- form
  readonly form = this.fb.nonNullable.group({
    displayName: this.fb.nonNullable.control<string>('', { validators: [Validators.required] }),
    bio: this.fb.nonNullable.control<string>(''),
    jobRole: this.fb.nonNullable.control<string>(''),
    avatarUrl: this.fb.nonNullable.control<string>(''),
  });

  // bridge reactive forms -> signals (for stable computed validity)
  private readonly formStatus = toSignal(
    this.form.statusChanges.pipe(startWith(this.form.status)),
    { initialValue: this.form.status }
  );

  private readonly displayNameValue = toSignal(
    this.form.controls.displayName.valueChanges.pipe(startWith(this.form.controls.displayName.value)),
    { initialValue: this.form.controls.displayName.value }
  );

  private readonly avatarUrlValue = toSignal(
    this.form.controls.avatarUrl.valueChanges.pipe(startWith(this.form.controls.avatarUrl.value)),
    { initialValue: this.form.controls.avatarUrl.value }
  );

  readonly displayNameError = computed(() => {
    const show = this.submitted() || this.form.controls.displayName.touched;
    if (!show) return null;

    const v = (this.displayNameValue() ?? '').trim();
    if (!v) return 'Display name is required.';
    return null;
  });

  readonly avatarUrlError = computed(() => {
    const show = this.submitted() || this.form.controls.avatarUrl.touched;
    if (!show) return null;

    const v = (this.avatarUrlValue() ?? '').trim();
    if (!v) return null;

    // lightweight URL validation (don’t overblock)
    const ok = v.startsWith('http://') || v.startsWith('https://');
    return ok ? null : 'Avatar URL must start with http:// or https://';
  });

  readonly canSubmit = computed(() => {
    if (this.loading() || this.saving()) return false;
    if (this.formStatus() !== 'VALID') return false;

    const dn = (this.displayNameValue() ?? '').trim();
    if (!dn) return false;

    // if avatar provided, it must pass our simple check
    if (this.avatarUrlError()) return false;

    return true;
  });

  constructor() {
    // Profile is auth required by route guard, but keep it defensive
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/account/login'], { queryParams: { returnUrl: '/profile' } });
      return;
    }

    // Load from shared store if already available; otherwise fetch
    this.currentUser.loadIfNeeded(this.destroyRef);

    const fromStore = this.currentUser.me();
    if (fromStore) {
      this.applyMeToForm(fromStore);
    } else {
      this.loadMe();
    }
  }

  // --------------------
  // data loading
  // --------------------
  private loadMe(): void {
    this.loading.set(true);
    this.error.set(null);
    this.errorCode.set(null);

    this.users
      .getMe()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (me) => {
          this.currentUser.set(me);
          this.applyMeToForm(me);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          const status = this.getHttpStatus(err);
          if (status === 401 || status === 403) {
            this.router.navigate(['/account/login'], { queryParams: { returnUrl: '/profile' } });
            return;
          }

          const { message, code } = this.extractProblem(err);
          this.error.set(message);
          this.errorCode.set(code);
          this.loading.set(false);
        },
      });
  }

  private applyMeToForm(me: GetCurrentUserResponse): void {
    this.form.patchValue({
      displayName: me.displayName ?? '',
      bio: me.bio ?? '',
      jobRole: me.jobRole ?? '',
      avatarUrl: me.avatarUrl ?? '',
    });

    this.interests.set(Array.isArray(me.interests) ? me.interests : []);
  }

  // --------------------
  // interests
  // --------------------
  addInterestFromDraft(): void {
    const raw = this.interestDraft().trim();
    if (!raw) return;

    // enforce one-by-one (same rule as tags)
    if (raw.includes(',')) return;

    const next = new Set(this.interests());
    next.add(raw);

    this.interests.set(Array.from(next));
    this.interestDraft.set('');
  }

  removeInterest(value: string): void {
    const v = (value ?? '').trim();
    if (!v) return;
    this.interests.set(this.interests().filter(x => x !== v));
  }

  // --------------------
  // save
  // --------------------
  save(): void {
    this.submitted.set(true);
    this.error.set(null);
    this.errorCode.set(null);

    if (!this.canSubmit()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const req: UpdateProfileRequest = {
      displayName: (this.form.controls.displayName.value ?? '').trim(),
      bio: this.nullIfBlank(this.form.controls.bio.value),
      jobRole: this.nullIfBlank(this.form.controls.jobRole.value),
      avatarUrl: this.nullIfBlank(this.form.controls.avatarUrl.value),
      interests: this.interests().map(x => x.trim()).filter(Boolean),
    };

    this.users
      .updateMe(req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          // update shared store so header/avatar updates instantly
          this.currentUser.set(updated);

          // re-apply to form (normalize)
          this.applyMeToForm(updated);

          this.saving.set(false);
          this.submitted.set(false);
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.error.set(message);
          this.errorCode.set(code);
          this.saving.set(false);
        },
      });
  }

  cancel(): void {
    // revert to latest store snapshot
    const me = this.currentUser.me();
    if (me) this.applyMeToForm(me);
    this.submitted.set(false);
    this.error.set(null);
    this.errorCode.set(null);
  }

  // --------------------
  // helpers
  // --------------------
  private nullIfBlank(v: string): string | null {
    const x = (v ?? '').trim();
    return x.length ? x : null;
  }

  private extractProblem(err: unknown): { message: string; code: string | null } {
    const anyErr = err as any;
    const problem: ApiProblem | undefined = anyErr?.error;

    const message =
      (typeof problem?.detail === 'string' && problem.detail.trim()) ||
      (typeof anyErr?.message === 'string' && anyErr.message.trim()) ||
      'Request failed.';

    const code = (typeof problem?.code === 'string' && problem.code.trim()) || null;
    return { message, code };
  }

  private getHttpStatus(err: unknown): number | null {
    const anyErr = err as any;
    if (typeof anyErr?.status === 'number') return anyErr.status;

    const problem: ApiProblem | undefined = anyErr?.error;
    if (typeof problem?.status === 'number') return problem.status;

    return null;
  }
}
