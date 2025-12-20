import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { PostType } from '../../../../shared/contracts/post/post-type';
import { FeedQuery, FeedService } from '../../services/feed-service';
import { AuthorDisplayNameCache } from '../../../../core/users/author-name-cache';


type ApiProblem = {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
};

@Component({
  selector: 'sonic-feed-page',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './feed-page.html',
  styleUrl: './feed-page.scss',
})
export class FeedPage {
  private readonly feedService = inject(FeedService);
  private readonly authorCache = inject(AuthorDisplayNameCache);
  private readonly destroyRef = inject(DestroyRef);

  // ---- UI state
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly errorCode = signal<string | null>(null);

  // ---- paging
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalItems = signal(0);
  readonly items = signal<readonly PostResponse[]>([]);
  readonly canLoadMore = computed(() => this.items().length < this.totalItems());

  // ---- filters
  readonly q = signal('');
  readonly featuredOnly = signal(false);
  readonly selectedType = signal<PostType | null>(null);

  // tags: one-by-one only (no comma split)
  readonly selectedTags = signal<readonly string[]>([]);
  readonly tagDraft = signal('');

  // ---- author display names
  readonly authorNames = signal<Record<string, string>>({});
  private readonly authorRequested = new Set<string>();

  readonly typeOptions: readonly PostType[] = [
    PostType.Experience,
    PostType.Idea,
    PostType.ModelGuide,
    PostType.Course,
    PostType.News,
    PostType.Campaign,
  ];

  constructor() {
    this.refresh();
  }

  // --------------------
  // actions
  // --------------------
  refresh(): void {
    this.page.set(1);
    this.items.set([]);
    this.fetchPage({ append: false });
  }

  loadMore(): void {
    if (this.loading() || !this.canLoadMore()) return;
    this.page.set(this.page() + 1);
    this.fetchPage({ append: true });
  }

  setType(type: PostType | null): void {
    this.selectedType.set(this.selectedType() === type ? null : type);
    this.refresh();
  }

  toggleFeatured(): void {
    this.featuredOnly.set(!this.featuredOnly());
    this.refresh();
  }

  onSearchEnter(): void {
    this.refresh();
  }

  addTagFromDraft(): void {
    const tag = this.tagDraft().trim();
    if (!tag) return;

    if (this.selectedTags().includes(tag)) {
      this.tagDraft.set('');
      return;
    }

    this.selectedTags.set([...this.selectedTags(), tag]);
    this.tagDraft.set('');
    this.refresh();
  }

  removeTag(tag: string): void {
    this.selectedTags.set(this.selectedTags().filter(t => t !== tag));
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

  authorName(authorId: string): string {
    const map = this.authorNames();
    return map[authorId] ?? '…';
  }

  // --------------------
  // internal
  // --------------------
  private fetchPage(opts: { append: boolean }): void {
    this.loading.set(true);
    this.error.set(null);
    this.errorCode.set(null);

    const query: FeedQuery = {
      page: this.page(),
      pageSize: this.pageSize(),
      type: this.selectedType(),
      tags: this.selectedTags(),
      q: this.q(),
      featured: this.featuredOnly() ? true : null,
    };

    this.feedService
      .getFeed(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const nextItems = opts.append ? [...this.items(), ...res.items] : res.items;
          this.items.set(nextItems);
          this.totalItems.set(res.totalItems);

          this.prefetchAuthorNames(nextItems);

          this.loading.set(false);
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.error.set(message);
          this.errorCode.set(code);
          this.loading.set(false);
        },
      });
  }

  private prefetchAuthorNames(posts: readonly PostResponse[]): void {
    for (const p of posts) {
      const authorId = (p.authorId ?? '').trim();
      if (!authorId) continue;

      // already loaded
      if (this.authorNames()[authorId]) continue;

      // already requested (avoid spamming)
      if (this.authorRequested.has(authorId)) continue;
      this.authorRequested.add(authorId);

      this.authorCache
        .getDisplayName(authorId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (name) => {
            // IMPORTANT: name is string | null. Only assign if string.
            if (!name) return;

            const current = this.authorNames();
            if (current[authorId] === name) return;

            this.authorNames.set({ ...current, [authorId]: name });
          },
        });
    }
  }

  private extractProblem(err: unknown): { message: string; code: string | null } {
    const anyErr = err as any;
    const problem: ApiProblem | undefined = anyErr?.error;

    const message =
      (typeof problem?.detail === 'string' && problem.detail.trim()) ||
      'Failed to load feed.';

    const code =
      (typeof problem?.code === 'string' && problem.code.trim()) ||
      null;

    return { message, code };
  }
}
