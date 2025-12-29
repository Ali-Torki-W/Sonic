import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { AuthStateService } from '../../../../core/auth/auth-state.service';
import { CampaignsQuery, CampaignsService } from '../../../../core/campaign/campaign.service';
import { ProblemDetails } from '../../../../core/http/problem-details';

@Component({
  selector: 'sonic-campaigns-page',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './campaigns-page.html',
  styleUrl: './campaigns-page.scss',
})
export class CampaignsPage {
  private readonly router = inject(Router);
  private readonly campaigns = inject(CampaignsService);
  private readonly authState = inject(AuthStateService);
  private readonly destroyRef = inject(DestroyRef);

  // --- UI State ---
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // --- Data ---
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalItems = signal(0);
  readonly items = signal<readonly PostResponse[]>([]);

  // --- Filters ---
  readonly q = signal('');
  readonly featuredOnly = signal(false);
  readonly selectedTags = signal<readonly string[]>([]);
  readonly tagDraft = signal('');

  // --- Computed Helpers ---
  readonly canLoadMore = computed(() => !this.loading() && this.items().length < this.totalItems());
  readonly isAuthed = computed(() => this.authState.isAuthenticated());

  // --- Join Status State ---
  // Maps to track status per Card ID { [id]: boolean/string }
  readonly joinBusyMap = signal<Record<string, boolean>>({});
  readonly joinedMap = signal<Record<string, boolean>>({});
  readonly joinErrorMap = signal<Record<string, string>>({});

  // Cache to prevent duplicate checks for the same ID in this session
  private readonly statusChecked = new Set<string>();

  constructor() {
    this.refresh();
  }

  // --- Actions ---

  refresh(): void {
    this.page.set(1);
    this.items.set([]); // Clear list on full refresh
    this.fetchPage({ append: false });
  }

  loadMore(): void {
    if (this.loading()) return;
    this.page.update(p => p + 1);
    this.fetchPage({ append: true });
  }

  onSearchEnter(): void { this.refresh(); }

  toggleFeatured(): void {
    this.featuredOnly.update(v => !v);
    this.refresh();
  }

  addTagFromDraft(): void {
    const t = this.tagDraft().trim();
    if (!t || t.includes(',')) return;

    this.selectedTags.update(tags => tags.includes(t) ? tags : [...tags, t]);
    this.tagDraft.set('');
    this.refresh();
  }

  removeTag(tag: string): void {
    this.selectedTags.update(tags => tags.filter(x => x !== tag));
    this.refresh();
  }

  clearFilters(): void {
    this.q.set('');
    this.featuredOnly.set(false);
    this.selectedTags.set([]);
    this.tagDraft.set('');
    this.refresh();
  }

  // --- Business Logic ---

  joinCampaign(postId: string): void {
    if (!postId) return;

    if (!this.isAuthed()) {
      this.router.navigate(['/account/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    if (this.joinBusyMap()[postId] || this.joinedMap()[postId]) return;

    // Set Optimistic / Busy UI
    this.setMapValue(this.joinBusyMap, postId, true);
    this.setMapValue(this.joinErrorMap, postId, null);

    this.campaigns.join(postId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.setMapValue(this.joinBusyMap, postId, false))
      )
      .subscribe({
        next: (resp) => {
          this.setMapValue(this.joinedMap, postId, true);

          // Update the card's participant count locally
          this.items.update(current =>
            current.map(p => p.id === postId ? { ...p, participantsCount: Number(resp.participantsCount) } : p)
          );
        },
        error: (err: any) => {
          const pd = err.error as ProblemDetails;
          this.setMapValue(this.joinErrorMap, postId, pd.detail || 'Unable to join mission.');
        }
      });
  }

  private fetchPage(opts: { append: boolean }): void {
    this.loading.set(true);
    this.error.set(null);

    const query: CampaignsQuery = {
      page: this.page(),
      pageSize: this.pageSize(),
      tags: this.selectedTags(),
      q: this.q(),
      featured: this.featuredOnly() || null,
    };

    this.campaigns.getCampaigns(query)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (result) => {
          const incoming = result.items ?? [];

          if (opts.append) {
            this.items.update(curr => [...curr, ...incoming]);
          } else {
            this.items.set(incoming);
          }
          this.totalItems.set(result.totalItems ?? 0);

          // Resolve status for new items
          this.resolveJoinedForVisible(incoming);
        },
        error: (err: any) => {
          const pd = err.error as ProblemDetails;
          this.error.set(pd.detail || 'Failed to retrieve campaigns.');
        }
      });
  }

  /**
   * Checks "Joined" status for visible cards.
   * Uses a Set to ensure we don't re-check the same ID multiple times.
   */
  private resolveJoinedForVisible(posts: readonly PostResponse[]): void {
    if (!this.isAuthed()) return;

    for (const p of posts) {
      // Skip if already joined (known) or already checked in this session
      if (!p.id || this.joinedMap()[p.id] || this.statusChecked.has(p.id)) continue;

      this.statusChecked.add(p.id);

      this.campaigns.getJoinStatus(p.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (resp) => {
            if (resp.joined) {
              this.setMapValue(this.joinedMap, p.id, true);
            }
            // Sync count (Server is source of truth)
            this.items.update(curr =>
              curr.map(x => x.id === p.id ? { ...x, participantsCount: Number(resp.participantsCount) } : x)
            );
          }
        });
    }
  }

  // Type-Safe Map Updater
  private setMapValue<T>(signalMap: any, key: string, value: T | null): void {
    signalMap.update((map: Record<string, T>) => {
      const next = { ...map };
      if (value === null) delete next[key];
      else next[key] = value;
      return next;
    });
  }
}