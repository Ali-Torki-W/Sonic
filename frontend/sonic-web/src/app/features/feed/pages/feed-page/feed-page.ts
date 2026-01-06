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
import { UsersService } from '../../../../core/users/user-service';
import { PostsService } from '../../../../core/posts/post.service';
import { AuthStateService } from '../../../../core/auth/auth-state.service';

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
  private readonly destroyRef = inject(DestroyRef);

  // NEW: Inject services for deleting and permissions
  private readonly postsService = inject(PostsService);
  private readonly usersService = inject(UsersService);
  private readonly authState = inject(AuthStateService);

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

  // --- User Permissions State ---
  readonly meId = signal<string | null>(null);
  readonly meRole = signal<string | null>(null);

  // --- Computed Helpers ---
  readonly typeOptions = Object.values(PostType);

  readonly canLoadMore = computed(() => {
    return !this.loading() && this.items().length < this.totalItems();
  });

  constructor() {
    // 1. Load User Info for Permissions
    this.loadUser();

    // 2. Load Feed
    this.refresh();

    // 3. Reactive Effect: Resolve Author Names
    effect(() => {
      const posts = this.items();
      const currentCache = untracked(this.authorNames);

      posts.forEach(p => {
        if (!p.authorId || currentCache[p.authorId]) return;

        this.authorCache.getDisplayName(p.authorId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(name => {
            if (name) {
              this.authorNames.update(map => ({ ...map, [p.authorId]: name }));
            }
          });
      });
    });
  }

  // --- Auth/User Logic ---
  private loadUser() {
    if (this.authState.isAuthenticated()) {
      this.usersService.getMe()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(me => {
          this.meId.set(me.id);
          this.meRole.set(me.role);
        });
    }
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

  // NEW: Delete Functionality
  deletePost(event: Event, post: PostResponse): void {
    // Stop the click from propagating to the RouterLink on the card
    event.stopPropagation();
    event.preventDefault();

    if (!confirm('Are you sure you want to delete this post?')) return;

    this.postsService.delete(post.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          // Optimistic UI Update: Remove from list immediately
          this.items.update(currentItems => currentItems.filter(p => p.id !== post.id));
          this.totalItems.update(c => Math.max(0, c - 1));
        },
        error: (err) => {
          alert("Failed to delete post.");
        }
      });
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