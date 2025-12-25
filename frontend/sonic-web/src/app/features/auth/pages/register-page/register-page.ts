import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AuthStateService } from '../../../../core/auth/auth-state.service';
import type { ProblemDetails } from '../../../../core/http/problem-details';
import { AuthService } from '../../../../core/auth/auth.service';
import { RegisterRequest } from '../../../../shared/contracts/auth/register-request';

@Component({
  selector: 'sonic-register-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register-page.html',
  styleUrl: './register-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly authApi = inject(AuthService);
  private readonly authState = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);

  private readonly emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
  private readonly passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,64}$/;

  readonly displayName = signal('');
  readonly email = signal('');
  readonly password = signal('');
  readonly confirmPassword = signal('');
  readonly showPassword = signal(false);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly passwordMismatch = computed(() => {
    const p = this.password();
    const c = this.confirmPassword();
    return p.length > 0 && c.length > 0 && p !== c;
  });

  readonly emailInvalid = computed(() => {
    const e = this.email().trim();
    return e.length > 0 && !this.emailRegex.test(e);
  });

  readonly passwordInvalid = computed(() => {
    const p = this.password();
    return p.length > 0 && !this.passwordRegex.test(p);
  });

  readonly clientValidationError = computed((): string | null => {
    if (this.displayName().trim().length === 0) return 'Display name is required.';
    if (this.email().trim().length === 0) return 'Email is required.';
    if (this.emailInvalid()) return 'Email is invalid (example: name@domain.com).';
    if (this.password().length === 0) return 'Password is required.';
    if (this.passwordInvalid()) return 'Password must be 8-64 chars and include uppercase, lowercase, and a number.';
    if (this.confirmPassword().length === 0) return 'Confirm password is required.';
    if (this.passwordMismatch()) return 'Passwords do not match.';
    return null;
  });

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  async submit(): Promise<void> {
    const clientErr = this.clientValidationError();
    if (clientErr) {
      this.error.set(clientErr);
      this.snack.open(clientErr, 'OK', { duration: 3500 });
      return;
    }

    this.error.set(null);
    this.loading.set(true);

    const payload: RegisterRequest = {
      email: this.email().trim(),
      password: this.password(),
      displayName: this.displayName().trim(),
    };

    try {
      const resp = await firstValueFrom(this.authApi.register(payload));

      // Auto-login on successful register (backend returns token).
      this.authState.setSession(resp.accessToken, resp.expiresAtUtc);

      const returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl') ?? '/profile';
      await this.router.navigateByUrl(returnUrl);
    } catch (err: any) {
      // HttpClient "status 0" is network/CORS
      if (err?.status === 0) {
        const msg = 'Network/CORS error: cannot reach API.';
        this.error.set(msg);
        this.snack.open(msg, 'OK', { duration: 3500 });
        return;
      }

      const pd = this.tryReadProblemDetails(err);
      const msg =
        firstValidationMessage(pd) ??
        (pd?.detail?.trim() ? pd.detail.trim() : null) ??
        (pd?.title?.trim() ? pd.title.trim() : null) ??
        statusFallback(err?.status) ??
        'Registration failed.';

      this.error.set(msg);
      this.snack.open(msg, 'OK', { duration: 4500 });
    } finally {
      this.loading.set(false);
    }
  }

  private tryReadProblemDetails(err: any): ProblemDetails | null {
    const body = err?.error;
    if (!body) return null;

    if (typeof body === 'string') {
      try {
        return JSON.parse(body) as ProblemDetails;
      } catch {
        // If it's plain text from backend, treat it as detail
        return { detail: body } as ProblemDetails;
      }
    }

    if (typeof body === 'object') return body as ProblemDetails;
    return null;
  }
}

function firstValidationMessage(pd: ProblemDetails | null): string | null {
  const errors = pd?.errors;
  if (!errors) return null;

  const keys = Object.keys(errors);
  if (keys.length === 0) return null;

  const firstKey = keys[0];
  const firstMsg = errors[firstKey]?.[0];
  return (typeof firstMsg === 'string' && firstMsg.trim()) ? firstMsg.trim() : null;
}

function statusFallback(status: number | null | undefined): string | null {
  if (status === 400) return 'Invalid registration data.';
  if (status === 401) return 'Unauthorized. Please log in.';
  if (status === 403) return 'Forbidden.';
  return null;
}
