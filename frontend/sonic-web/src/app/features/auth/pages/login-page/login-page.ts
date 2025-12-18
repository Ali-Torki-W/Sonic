import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { AuthStateService } from '../../../../core/auth/auth-state.service';
import type { ProblemDetails } from '../../../../core/http/problem-details';
import { AuthService } from '../../../../core/auth/auth.service';
import { LoginRequest } from '../../../../shared/contracts/auth/login-request';

// TODO: replace with your real DTO export path (no DTO variants, no guessing).

@Component({
  selector: 'sonic-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly authApi = inject(AuthService);
  private readonly authState = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  readonly email = signal('');
  readonly password = signal('');
  readonly showPassword = signal(false);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  async submit(): Promise<void> {
    if (this.loading()) return;

    this.error.set(null);
    this.loading.set(true);

    const payload: LoginRequest = {
      email: this.email(),
      password: this.password(),
    };

    try {
      const resp = await firstValueFrom(this.authApi.login(payload));

      // IMPORTANT: expiresAtUtc must be treated as ISO string in TS (DateTime -> string).
      this.authState.setSession(resp.accessToken, resp.expiresAtUtc);

      const returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl') ?? '/feed';
      await this.router.navigateByUrl(returnUrl);
    } catch (err: any) {
      // Common when API is unreachable / CORS is misconfigured:
      if (err?.status === 0) {
        this.error.set('Network/CORS error: cannot reach API.');
        return;
      }

      const pd = this.tryReadProblemDetails(err);
      this.error.set(pd?.detail ?? 'Login failed.');
    } finally {
      this.loading.set(false);
    }
  }

  private tryReadProblemDetails(err: any): ProblemDetails | null {
    const body = err?.error;

    if (!body) return null;

    // Sometimes Angular gives you a string body even when the server returned JSON.
    if (typeof body === 'string') {
      try {
        return JSON.parse(body) as ProblemDetails;
      } catch {
        return null;
      }
    }

    if (typeof body === 'object') {
      // Your middleware returns { title, status, detail, instance, type, code }
      return body as ProblemDetails;
    }

    return null;
  }
}
