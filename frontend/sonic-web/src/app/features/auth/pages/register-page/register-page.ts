import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

import { AuthService } from '../../../../core/auth/auth.service';
import { RegisterRequest } from '../../../../shared/contracts/auth/register-request';
import type { ProblemDetails } from '../../../../core/http/problem-details';

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
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);

  // Constants
  private readonly emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
  private readonly passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,64}$/;

  // State
  readonly displayName = signal('');
  readonly email = signal('');
  readonly password = signal('');
  readonly confirmPassword = signal('');
  readonly showPassword = signal(false);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // Computed Validation
  readonly passwordMismatch = computed(() => {
    return this.password() && this.confirmPassword() && this.password() !== this.confirmPassword();
  });

  readonly validationError = computed(() => {
    if (!this.displayName().trim()) return 'Display name is required.';

    const e = this.email().trim();
    if (!e) return 'Email is required.';
    if (!this.emailRegex.test(e)) return 'Email is invalid.';

    const p = this.password();
    if (!p) return 'Password is required.';
    if (!this.passwordRegex.test(p)) return 'Password must be 8-64 chars (Upper, Lower, Digit).';

    if (this.passwordMismatch()) return 'Passwords do not match.';

    return null;
  });

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  async submit(): Promise<void> {
    const validationErr = this.validationError();
    if (validationErr) {
      this.showError(validationErr);
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
      await firstValueFrom(this.authApi.register(payload));

      const returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl') ?? '/profile';
      await this.router.navigateByUrl(returnUrl);
    } catch (err: any) {
      // TRUST THE INTERCEPTOR:
      // The interceptor has already normalized 400 Validation Errors,
      // 0 Network Errors, and 500 Server Errors into a clean ProblemDetails.
      const pd = err.error as ProblemDetails;

      // Even if it was a complex validation error, our interceptor 
      // moved the first error message into `pd.detail`.
      const msg = pd.detail || 'Registration failed.';

      this.showError(msg);
    } finally {
      this.loading.set(false);
    }
  }

  private showError(msg: string) {
    this.error.set(msg);
    this.snack.open(msg, 'OK', { duration: 4500 });
  }
}