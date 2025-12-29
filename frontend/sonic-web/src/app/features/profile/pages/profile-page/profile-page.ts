import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { map, startWith } from 'rxjs/operators';

import { UsersService } from '../../../../core/users/user-service';
import { CurrentUserStore } from '../../../../core/users/current-user-store';
import { AuthStateService } from '../../../../core/auth/auth-state.service';
import { GetCurrentUserResponse } from '../../../../shared/contracts/user/get-current-user-response';
import { UpdateProfileRequest } from '../../../../shared/contracts/user/update-profile-request';

// Moved to a const for reusability/clarity
const URL_VALIDATOR: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const v = (control.value || '').trim();
  if (!v) return null; // Let required validator handle empty checks if needed
  const valid = v.startsWith('http://') || v.startsWith('https://');
  return valid ? null : { invalidUrl: true };
};

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
export class ProfilePage implements OnInit {
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

  // ---- interests
  readonly interests = signal<readonly string[]>([]);
  readonly interestDraft = signal('');

  // ---- form
  readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required]],
    bio: [''],
    jobRole: [''],
    avatarUrl: ['', [URL_VALIDATOR]], // Native validation!
  });

  // ---- Signal Bridges
  // We only bridge what we need for the template to react to
  private readonly formStatus = toSignal(
    this.form.statusChanges.pipe(startWith(this.form.status)),
    { initialValue: this.form.status }
  );

  // Helper to detect if we should show errors (Touched OR Submitted)
  private readonly showErrors = computed(() =>
    this.submitted() || this.formStatus() !== 'PENDING' // simplified trigger
  );

  readonly displayNameError = computed(() => {
    // We rely on the form control's internal error map now
    if (!this.showErrors() && !this.form.controls.displayName.touched) return null;
    if (this.form.controls.displayName.hasError('required')) return 'Display name is required.';
    return null;
  });

  readonly avatarUrlError = computed(() => {
    if (!this.showErrors() && !this.form.controls.avatarUrl.touched) return null;
    // The ValidatorFn logic above sets this error key
    if (this.form.controls.avatarUrl.hasError('invalidUrl')) return 'Avatar URL must start with http:// or https://';
    return null;
  });

  readonly canSubmit = computed(() => {
    if (this.loading() || this.saving()) return false;
    // Now we can trust the native form validity
    return this.formStatus() === 'VALID';
  });

  constructor() {
    // Optional: Use effect to sync store changes automatically if the store updates elsewhere
    // effect(() => {
    //   const me = this.currentUser.me();
    //   if (me && !this.form.dirty) this.applyMeToForm(me);
    // });
  }

  ngOnInit(): void {
    // 1. Guard Logic (Defensive)
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/account/login'], { queryParams: { returnUrl: '/profile' } });
      return;
    }

    // 2. Data Loading Strategy
    this.currentUser.loadIfNeeded();
    const fromStore = this.currentUser.me();

    if (fromStore) {
      this.applyMeToForm(fromStore);
    } else {
      this.loadMe();
    }
  }

  // --------------------
  // Actions
  // --------------------

  addInterestFromDraft(): void {
    const raw = this.interestDraft().trim();
    if (!raw || raw.includes(',')) return;

    // Use update for cleaner set logic
    this.interests.update(current => {
      const next = new Set(current);
      next.add(raw);
      return Array.from(next);
    });

    this.interestDraft.set('');
  }

  removeInterest(value: string): void {
    this.interests.update(current => current.filter(x => x !== value));
  }

  save(): void {
    this.submitted.set(true);
    this.error.set(null);
    this.errorCode.set(null);

    // Form validation check is now aligned with Native Forms
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const formVal = this.form.getRawValue();

    const req: UpdateProfileRequest = {
      displayName: formVal.displayName.trim(),
      bio: this.nullIfBlank(formVal.bio),
      jobRole: this.nullIfBlank(formVal.jobRole),
      avatarUrl: this.nullIfBlank(formVal.avatarUrl),
      interests: this.interests().map(x => x.trim()).filter(Boolean),
    };

    this.users.updateMe(req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.currentUser.set(updated);
          this.applyMeToForm(updated); // Resets 'dirty' state conceptually
          this.saving.set(false);
          this.submitted.set(false);
          this.form.markAsPristine(); // Important: Mark form pristine after save
        },
        error: (err) => this.handleError(err)
      });
  }

  cancel(): void {
    const me = this.currentUser.me();
    if (me) {
      this.applyMeToForm(me);
      this.form.markAsPristine();
    }
    this.submitted.set(false);
    this.error.set(null);
  }

  // --------------------
  // Helpers
  // --------------------

  private loadMe(): void {
    this.loading.set(true);
    this.users.getMe()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (me) => {
          this.currentUser.set(me);
          this.applyMeToForm(me);
          this.loading.set(false);
        },
        error: (err) => this.handleError(err)
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

  private handleError(err: unknown): void {
    this.loading.set(false);
    this.saving.set(false);

    const status = this.getHttpStatus(err);
    if (status === 401 || status === 403) {
      this.router.navigate(['/account/login'], { queryParams: { returnUrl: '/profile' } });
      return;
    }

    const { message, code } = this.extractProblem(err);
    this.error.set(message);
    this.errorCode.set(code);
  }

  private nullIfBlank(v: string): string | null {
    return (v || '').trim() || null;
  }

  private extractProblem(err: any): { message: string; code: string | null } {
    const problem: ApiProblem = err?.error;
    return {
      message: problem?.detail || err?.message || 'Request failed.',
      code: problem?.code || null
    };
  }

  private getHttpStatus(err: any): number | null {
    return err?.status || err?.error?.status || null;
  }
}