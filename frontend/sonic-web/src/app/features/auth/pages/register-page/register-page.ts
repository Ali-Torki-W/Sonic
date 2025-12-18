import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

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

  readonly canSubmit = computed(() => {
    if (this.loading()) return false;
    if (this.passwordMismatch()) return false;
    return this.displayName().trim().length > 0
      && this.email().trim().length > 0
      && this.password().length > 0
      && this.confirmPassword().length > 0;
  });

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  async submit(): Promise<void> {
    if (!this.canSubmit()) return;

    this.error.set(null);
    this.loading.set(true);

    const payload: RegisterRequest = {
      email: this.email().trim(),
      password: this.password(),
      displayName: this.displayName().trim(),
    };

    try {
      const resp = await firstValueFrom(this.authApi.register(payload));

      // Auto-login on successful register (your backend returns token).
      this.authState.setSession(resp.accessToken, resp.expiresAtUtc);

      const returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl') ?? '/feed';
      await this.router.navigateByUrl(returnUrl);
    } catch (err: any) {
      if (err?.status === 0) {
        this.error.set('Network/CORS error: cannot reach API.');
        return;
      }

      const pd = this.tryReadProblemDetails(err);
      this.error.set(pd?.detail ?? 'Registration failed.');
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
        return null;
      }
    }

    if (typeof body === 'object') return body as ProblemDetails;
    return null;
  }
}
