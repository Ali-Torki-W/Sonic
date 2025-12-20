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
    { label: 'Explore', path: '/feed' },
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
    this.auth.clearSession();
    this.closeMobile();
  }

  // for new added designs
  // In your component TypeScript file
  ngAfterViewInit() {
    this.createStars();
  }

  createStars() {
    const starsContainer = document.querySelector('.stars');
    if (!starsContainer) return;

    // Clear existing stars
    starsContainer.innerHTML = '';

    // Create 150 stars
    for (let i = 0; i < 150; i++) {
      const star = document.createElement('div');
      star.className = 'star';

      // Random position
      const left = Math.random() * 100;
      const top = Math.random() * 100;

      // Random size (0.5px to 2px)
      const size = 0.5 + Math.random() * 1.5;

      // Random animation delay
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
}
