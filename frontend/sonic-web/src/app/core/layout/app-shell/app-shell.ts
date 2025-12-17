import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthStateService } from '../../auth/auth-state.service';

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
export class AppShell {
  private readonly auth = inject(AuthStateService);

  readonly mobileOpen = signal(false);

  readonly isAuthenticated = computed(() => this.auth.isAuthenticated());

  readonly navLinks: NavLink[] = [
    { label: 'Feed', path: '/feed' },
    { label: 'Experiences', path: '/feed', queryParams: { type: 'Experience' } },
    { label: 'Ideas', path: '/feed', queryParams: { type: 'Idea' } },
    { label: 'Models', path: '/feed', queryParams: { type: 'ModelGuide' } },
    { label: 'Courses', path: '/feed', queryParams: { type: 'Course' } },
    { label: 'News', path: '/feed', queryParams: { type: 'News' } },
    { label: 'Campaigns', path: '/campaigns' },
  ];

  toggleMobile(): void {
    this.mobileOpen.update((v) => !v);
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
    this.closeMobile();
  }
}
