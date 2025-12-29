import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { LoginRequest } from '../../../../shared/contracts/auth/login-request';
import type { ProblemDetails } from '../../../../core/http/problem-details';

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
      await firstValueFrom(this.authApi.login(payload));

      const returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl') ?? '/feed';
      await this.router.navigateByUrl(returnUrl);
    }
    catch (err: any) {
      // TRUST THE INTERCEPTOR:
      // err.error is guaranteed to be a normalized ProblemDetails object.
      const pd = err.error as ProblemDetails;

      // The interceptor already calculated a friendly message into .detail
      this.error.set(pd.detail || 'Login failed.');
    }
    finally {
      this.loading.set(false);
    }
  }
}