import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { PostType } from '../../../../shared/contracts/post/post-type';
import { FeedQuery, FeedService } from '../../services/feed-service';
import { AuthorDisplayNameCache } from '../../../../core/users/author-display-name-cache';
import { ProblemDetails } from '../../../../core/http/problem-details';

@Component({
  selector: 'sonic-feed-page',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe, FormsModule],
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
})
export class FeedPage {
  private readonly feedService = inject(FeedService);
  private readonly authorCache = inject(AuthorDisplayNameCache);

  // ✅ FIX 1: Inject DestroyRef explicitly
  private readonly destroyRef = inject(DestroyRef);

  // --- UI State ---
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // --- Data State ---
  readonly page = signal(1);
  readonly items = signal<readonly PostResponse[]>([]);
  readonly totalItems = signal(0);

  // Author Display Names Cache
  readonly authorNames = signal<Record<string, string>>({});

  // --- Filter State ---
  readonly q = signal('');
  readonly featuredOnly = signal(false);
  readonly selectedType = signal<PostType | null>(null);
  readonly selectedTags = signal<readonly string[]>([]);
  readonly tagDraft = signal('');

  // --- Computed Helpers ---
  readonly typeOptions = Object.values(PostType);

  readonly canLoadMore = computed(() => {
    return !this.loading() && this.items().length < this.totalItems();
  });

  constructor() {
    this.refresh();

    // Reactive Effect: Resolve Author Names
    effect(() => {
      const posts = this.items();
      const currentCache = untracked(this.authorNames);

      posts.forEach(p => {
        if (!p.authorId || currentCache[p.authorId]) return;

        this.authorCache.getDisplayName(p.authorId)
          // ✅ FIX 2: Pass destroyRef explicitly inside effect
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(name => {
            if (name) {
              this.authorNames.update(map => ({ ...map, [p.authorId]: name }));
            }
          });
      });
    });
  }

  // --- Actions ---

  refresh(): void {
    this.page.set(1);
    this.executeFetch({ append: false });
  }

  loadMore(): void {
    if (this.loading()) return;
    this.page.update(p => p + 1);
    this.executeFetch({ append: true });
  }

  // --- Filter Logic ---

  onSearchEnter(): void { this.refresh(); }

  toggleFeatured(): void {
    this.featuredOnly.update(v => !v);
    this.refresh();
  }

  setType(type: PostType | null): void {
    this.selectedType.update(curr => (curr === type ? null : type));
    this.refresh();
  }

  addTagFromDraft(): void {
    const tag = this.tagDraft().trim();
    if (!tag) return;
    this.selectedTags.update(tags => tags.includes(tag) ? tags : [...tags, tag]);
    this.tagDraft.set('');
    this.refresh();
  }

  removeTag(tagToRemove: string): void {
    this.selectedTags.update(tags => tags.filter(t => t !== tagToRemove));
    this.refresh();
  }

  clearFilters(): void {
    this.q.set('');
    this.featuredOnly.set(false);
    this.selectedType.set(null);
    this.selectedTags.set([]);
    this.tagDraft.set('');
    this.refresh();
  }

  // --- UI Helpers ---

  getAuthorName(authorId: string): string {
    return this.authorNames()[authorId] || '...';
  }

  // --- Core Fetch Logic ---

  private executeFetch(opts: { append: boolean }): void {
    this.loading.set(true);
    this.error.set(null);

    const query: FeedQuery = {
      page: this.page(),
      pageSize: 10,
      q: this.q() || null,
      type: this.selectedType(),
      tags: this.selectedTags(),
      featured: this.featuredOnly() || null
    };

    this.feedService.getFeed(query)
      .pipe(
        // ✅ FIX 3: Pass destroyRef explicitly here too (fixes button click crash)
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (res) => {
          this.totalItems.set(res.totalItems);
          if (opts.append) {
            this.items.update(current => [...current, ...res.items]);
          } else {
            this.items.set(res.items);
          }
        },
        error: (err) => {
          const pd = err.error as ProblemDetails;
          this.error.set(pd?.detail || 'Unable to retrieve mission protocols.');
        }
      });
  }
}