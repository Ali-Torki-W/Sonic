import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  signal,
  computed,
  effect,
  NgZone,
  OnDestroy,
  ViewChild
} from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';

import { AuthStateService } from '../../auth/auth-state.service';
import { CurrentUserStore } from '../../users/current-user-store';

type NavLink = { label: string; path: string; exact?: boolean; };

interface StarParticle {
  element: HTMLElement;
  baseX: number;
  baseY: number;
  x: number;
  y: number;
  vx: number;
  vy: number;
  friction: number;
  ease: number;
  size: number;
}

@Component({
  selector: 'sonic-app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell implements AfterViewInit, OnDestroy {
  private readonly auth = inject(AuthStateService);
  private readonly currentUser = inject(CurrentUserStore);
  private readonly elementRef = inject(ElementRef);
  private readonly ngZone = inject(NgZone);

  readonly mobileOpen = signal(false);
  readonly isAuthenticated = computed(() => this.auth.isAuthenticated());

  readonly me = this.currentUser.me;
  readonly avatarUrl = this.currentUser.avatarUrl;
  readonly displayName = this.currentUser.displayName;

  readonly navLinks: NavLink[] = [
    { label: 'Explore', path: '/feed', exact: true },
    { label: 'Campaigns', path: '/campaigns' },
    { label: 'Experiences', path: '/experiences' },
    { label: 'Ideas', path: '/ideas' },
    { label: 'Models', path: '/models' },
    { label: 'Courses', path: '/courses' },
    { label: 'News', path: '/news' },
  ];

  // Animation State
  private stars: StarParticle[] = [];
  private mouse = { x: -9999, y: -9999 };
  private animationFrameId: number | null = null;
  private resizeObserver: ResizeObserver | null = null;

  @ViewChild('navContainer') navContainer?: ElementRef<HTMLElement>;

  constructor() {
    effect(() => {
      if (this.isAuthenticated()) {
        this.currentUser.loadIfNeeded();
      } else {
        this.currentUser.clear();
      }
    });
  }

  ngAfterViewInit(): void {
    // 1. Stars Physics
    this.createStars();
    this.ngZone.runOutsideAngular(() => {
      this.initStarInteraction();
      this.animateStars();
    });

    // 2. Nav Spotlight Effect
    this.initSpotlight();
  }

  ngOnDestroy(): void {
    if (this.animationFrameId) cancelAnimationFrame(this.animationFrameId);
    this.resizeObserver?.disconnect();
  }

  toggleMobile(): void { this.mobileOpen.update((v) => !v); }
  closeMobile(): void { this.mobileOpen.set(false); }

  logout(): void {
    this.auth.clearSession();
    this.currentUser.clear();
    this.closeMobile();
  }

  initial(): string {
    const name = (this.displayName() ?? '').trim();
    return name ? name.slice(0, 1).toUpperCase() : 'U';
  }

  // --- Feature: Spotlight Navigation ---
  // Tracks mouse over the nav bar to move a CSS gradient
  private initSpotlight(): void {
    this.ngZone.runOutsideAngular(() => {
      const nav = this.elementRef.nativeElement.querySelector('.nav-spotlight-group');
      if (!nav) return;

      nav.addEventListener('mousemove', (e: MouseEvent) => {
        const rect = nav.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        nav.style.setProperty('--mouse-x', `${x}px`);
        nav.style.setProperty('--mouse-y', `${y}px`);
      });
    });
  }

  // --- Feature: Liquid Star Physics ---

  private createStars(): void {
    const starsContainer = this.elementRef.nativeElement.querySelector('.stars');
    if (!starsContainer) return;

    starsContainer.innerHTML = '';
    this.stars = [];

    const count = 200;

    for (let i = 0; i < count; i++) {
      const star = document.createElement('div');
      star.className = 'star';

      const left = Math.random() * 100;
      const top = Math.random() * 100;
      const size = 1 + Math.random() * 2;
      const delay = Math.random() * 4;

      star.style.cssText = `
        position: absolute;
        left: ${left}%;
        top: ${top}%;
        width: ${size}px;
        height: ${size}px;
        /* ✅ CHANGED: Use a cyan-tinted white for better text contrast */
        background: #bfefff; 
        border-radius: 50%;
        opacity: ${0.2 + Math.random() * 0.5}; /* Lower opacity */
        animation: twinkle ${3 + Math.random() * 3}s infinite ${delay}s;
        will-change: transform;
      `;

      starsContainer.appendChild(star);

      this.stars.push({
        element: star,
        baseX: left,
        baseY: top,
        x: 0,
        y: 0,
        vx: 0,
        vy: 0,
        size: size,
        friction: 0.96,
        ease: 0.03
      });
    }
  }

  private initStarInteraction(): void {
    window.addEventListener('mousemove', (e) => {
      this.mouse.x = e.clientX;
      this.mouse.y = e.clientY;
    });
  }

  private animateStars(): void {
    const container = this.elementRef.nativeElement;
    const { width, height } = container.getBoundingClientRect();

    // Interaction Radius
    const radius = 200;

    this.stars.forEach(star => {
      const starPixelX = (star.baseX / 100) * width + star.x;
      const starPixelY = (star.baseY / 100) * height + star.y;

      const dx = this.mouse.x - starPixelX;
      const dy = this.mouse.y - starPixelY;
      const distance = Math.sqrt(dx * dx + dy * dy);

      // Repulsion Logic
      if (distance < radius) {
        const angle = Math.atan2(dy, dx);
        const force = (radius - distance) / radius;
        const push = force * 4; // Gentle push

        star.vx -= Math.cos(angle) * push;
        star.vy -= Math.sin(angle) * push;
      }

      // Spring back home
      star.vx += (0 - star.x) * star.ease;
      star.vy += (0 - star.y) * star.ease;

      // Friction
      star.vx *= star.friction;
      star.vy *= star.friction;

      // Apply
      star.x += star.vx;
      star.y += star.vy;

      star.element.style.transform = `translate3d(${star.x}px, ${star.y}px, 0)`;
    });

    this.animationFrameId = requestAnimationFrame(() => this.animateStars());
  }
}