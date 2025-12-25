import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthStateService } from '../../auth/auth-state.service';
import { CurrentUserStore } from '../../users/current-user-store';

type NavLink = {
  label: string;
  path: string;
  queryParams?: Record<string, unknown>;
};

@Component({
  selector: 'sonic-app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell implements AfterViewInit {
  private readonly auth = inject(AuthStateService);
  private readonly currentUser = inject(CurrentUserStore);
  private readonly destroyRef = inject(DestroyRef);

  readonly mobileOpen = signal(false);

  readonly isAuthenticated = computed(() => this.auth.isAuthenticated());

  // expose store signals to template
  readonly me = this.currentUser.me;
  readonly avatarUrl = this.currentUser.avatarUrl;
  readonly displayName = this.currentUser.displayName;

  readonly navLinks: NavLink[] = [
    { label: 'Explore', path: '/feed' },
    { label: 'Experiences', path: '/feed', queryParams: { type: 'Experience' } },
    { label: 'Ideas', path: '/feed', queryParams: { type: 'Idea' } },
    { label: 'Models', path: '/feed', queryParams: { type: 'ModelGuide' } },
    { label: 'Courses', path: '/feed', queryParams: { type: 'Course' } },
    { label: 'News', path: '/feed', queryParams: { type: 'News' } },
    { label: 'Campaigns', path: '/campaigns' },
  ];

  constructor() {
    // keep header avatar in sync with auth state (loads after login, clears after logout/expiry)
    effect(() => {
      if (this.isAuthenticated()) {
        this.currentUser.loadIfNeeded(this.destroyRef);
      } else {
        this.currentUser.clear();
      }
    });
  }

  toggleMobile(): void {
    this.mobileOpen.update((v) => !v);
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }

  logout(): void {
    this.auth.clearSession();
    this.currentUser.clear();
    this.closeMobile();
  }

  // ---- your stars logic (kept)
  ngAfterViewInit(): void {
    this.createStars();
  }

  createStars(): void {
    const starsContainer = document.querySelector('.stars');
    if (!starsContainer) return;

    starsContainer.innerHTML = '';

    for (let i = 0; i < 150; i++) {
      const star = document.createElement('div');
      star.className = 'star';

      const left = Math.random() * 100;
      const top = Math.random() * 100;
      const size = 0.5 + Math.random() * 1.5;
      const delay = Math.random() * 3;

      star.style.cssText = `
        position: absolute;
        left: ${left}%;
        top: ${top}%;
        width: ${size}px;
        height: ${size}px;
        background: white;
        border-radius: 50%;
        animation: twinkle ${2 + Math.random() * 4}s infinite ${delay}s;
        pointer-events: none;
      `;

      starsContainer.appendChild(star);
    }
  }

  // helper for initials fallback
  initial(): string {
    const name = (this.displayName() ?? '').trim();
    if (!name) return 'U';
    return name.slice(0, 1).toUpperCase();
  }
}
