import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { PostType } from '../../../../shared/contracts/post/post-type';
import { FeedQuery, FeedService } from '../../services/feed-service';
import { AuthorNameCache } from '../../../../core/users/author-name-cache';

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
  private readonly feed = inject(FeedService);
  private readonly authorNamesCache = inject(AuthorNameCache);
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

  // ---- author names resolved (id -> displayName)
  readonly authorNames = signal<Record<string, string>>({});

  // ---- filters
  readonly q = signal('');
  readonly featuredOnly = signal(false);
  readonly selectedType = signal<PostType | null>(null);
  readonly selectedTags = signal<readonly string[]>([]);
  readonly tagDraft = signal('');

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

  setSearch(value: string): void {
    this.q.set(value);
  }

  searchNow(): void {
    this.refresh();
  }

  setTagDraft(value: string): void {
    this.tagDraft.set(value);
  }

  addTag(): void {
    const raw = this.tagDraft().trim();
    if (!raw) return;

    // strict: add ONE tag only (no commas)
    if (raw.includes(',')) {
      this.error.set('Add tags one by one (no commas).');
      this.errorCode.set('ui.tags.single_only');
      return;
    }

    const normalized = this.normalizeTag(raw);
    if (!normalized) return;

    const current = this.selectedTags();
    const exists = current.some(t => t.toLowerCase() === normalized.toLowerCase());
    if (exists) {
      this.tagDraft.set('');
      return;
    }

    this.selectedTags.set([...current, normalized]);
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

  authorLabel(authorId: string): string {
    const map = this.authorNames();
    return map[authorId] ?? this.shortId(authorId);
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

    this.feed
      .getFeed(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          const merged = opts.append ? [...this.items(), ...result.items] : result.items;

          this.items.set(merged);
          this.totalItems.set(result.totalItems);
          this.loading.set(false);

          this.resolveAuthors(merged);
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.error.set(message);
          this.errorCode.set(code);
          this.loading.set(false);
        },
      });
  }

  private resolveAuthors(posts: readonly PostResponse[]): void {
    const current = this.authorNames();
    const missing = new Set<string>();

    for (const p of posts) {
      const id = (p.authorId ?? '').trim();
      if (!id) continue;
      if (!current[id]) missing.add(id);
    }

    if (missing.size === 0) return;

    for (const authorId of missing) {
      this.authorNamesCache
        .getDisplayName(authorId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (name) => {
            const nextMap = { ...this.authorNames() };
            nextMap[authorId] = name;
            this.authorNames.set(nextMap);
          },
        });
    }
  }

  private normalizeTag(value: string): string {
    let t = value.trim();
    if (!t) return '';
    if (t.startsWith('#')) t = t.slice(1).trim();
    return t;
  }

  private extractProblem(err: unknown): { message: string; code: string | null } {
    const anyErr = err as any;
    const problem: ApiProblem | undefined = anyErr?.error;

    const message =
      (typeof problem?.detail === 'string' && problem.detail.trim()) ||
      'Failed to load posts.';

    const code =
      (typeof problem?.code === 'string' && problem.code.trim()) ||
      null;

    return { message, code };
  }

  private shortId(id: string): string {
    const s = (id ?? '').trim();
    return s.length > 10 ? `${s.slice(0, 6)}…${s.slice(-4)}` : s || 'Unknown';
  }
}
