import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { UsersService } from '../../../../core/users/user-service';
import { PublicProfileResponse } from '../../../../shared/contracts/user/public-profile-response';

@Component({
  selector: 'app-public-profile-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './public-profile.html',
  styleUrl: './public-profile.scss',
})
export class PublicProfile {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly users = inject(UsersService);

  // State
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly profile = signal<PublicProfileResponse | null>(null);

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => {
        const id = params.get('id');
        if (id) {
          this.loadProfile(id);
        } else {
          this.router.navigate(['/not-found']);
        }
      });
  }

  private loadProfile(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.profile.set(null);

    this.users.getPublicProfile(id)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (data) => this.profile.set(data),
        error: (err) => {
          this.error.set(err?.error?.detail || 'Unable to decrypt personnel file.');
        }
      });
  }

  getInitials(name?: string): string {
    return (name || '?').charAt(0).toUpperCase();
  }
}