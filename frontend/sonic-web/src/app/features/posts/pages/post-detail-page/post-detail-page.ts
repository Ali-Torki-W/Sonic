import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { CommentResponse } from '../../../../shared/contracts/comment/create-comment.response';
import { PostType } from '../../../../shared/contracts/post/post-type';

import { PostsService } from '../../../../core/posts/post.service';
import { CommentsService } from '../../../../core/comments/comment.service';
import { UsersService } from '../../../../core/users/user-service';
import { AuthStateService } from '../../../../core/auth/auth-state.service';
import { CampaignsService } from '../../../../core/campaign/campaign.service';
import { ProblemDetails } from '../../../../core/http/problem-details';
import { AuthorDisplayNameCache } from '../../../../core/users/author-display-name-cache';

@Component({
  selector: 'sonic-post-detail-page',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe, FormsModule],
  templateUrl: './post-detail-page.html',
  styleUrl: './post-detail-page.scss',
})
export class PostDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly posts = inject(PostsService);
  private readonly comments = inject(CommentsService);
  private readonly authorCache = inject(AuthorDisplayNameCache);
  private readonly users = inject(UsersService);
  private readonly authState = inject(AuthStateService);
  private readonly campaigns = inject(CampaignsService);
  private readonly destroyRef = inject(DestroyRef);

  // --- Route Data ---
  readonly postId = signal<string>('');

  // --- Current User State ---
  readonly meId = signal<string | null>(null);
  readonly meRole = signal<string | null>(null);
  readonly meLoading = signal(false);
  readonly isAuthed = computed(() => this.authState.isAuthenticated());

  // --- Post State ---
  readonly postLoading = signal(false);
  readonly postError = signal<string | null>(null);
  readonly post = signal<PostResponse | null>(null);

  // Author Map: { "user-id": "Bob" }
  readonly authorNames = signal<Record<string, string>>({});

  // --- Actions State (Like/Join) ---
  readonly likeBusy = signal(false);
  readonly liked = signal(false);
  readonly likeToast = signal<string | null>(null);

  readonly joinBusy = signal(false);
  readonly joinedCampaign = signal(false);
  readonly joinToast = signal<string | null>(null);

  // --- Comments State ---
  readonly commentsLoading = signal(false);
  readonly commentsError = signal<string | null>(null);

  readonly commentsPage = signal(1);
  readonly commentsTotalItems = signal(0);
  readonly commentItems = signal<readonly CommentResponse[]>([]);
  readonly commentDraft = signal('');
  readonly creatingComment = signal(false);

  // --- Computed Helpers ---
  readonly canLoadMoreComments = computed(
    () => !this.commentsLoading() && this.commentItems().length < this.commentsTotalItems()
  );

  readonly isCampaign = computed(() => this.post()?.type === PostType.Campaign);

  readonly canEditPost = computed(() => {
    const p = this.post();
    if (!p || !this.isAuthed()) return false;

    const meId = this.meId();
    const role = (this.meRole() ?? '').toLowerCase();
    return (meId && meId === p.authorId) || role === 'admin';
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/not-found']);
      return;
    }

    this.postId.set(id);
    this.loadMeIfAuthenticated();
    this.loadPost();
    this.refreshComments();
  }

  // --- Navigation ---
  backToFeed(): void { this.router.navigate(['/feed']); }

  goToLogin(): void {
    this.router.navigate(['/account/login'], { queryParams: { returnUrl: this.router.url } });
  }

  goToEdit(): void {
    const p = this.post();
    if (p) this.router.navigate([`/posts/${p.id}/edit`]);
  }

  // --- Data Loading ---

  private loadMeIfAuthenticated(): void {
    if (!this.isAuthed()) return;
    this.meLoading.set(true);

    this.users.getMe()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.meLoading.set(false))
      )
      .subscribe({
        next: (me) => {
          this.meId.set(me.id);
          this.meRole.set(me.role);
          if (me.displayName) this.updateAuthorMap(me.id, me.displayName);
        }
      });
  }

  private loadPost(): void {
    this.postLoading.set(true);
    this.postError.set(null);

    this.posts.getById(this.postId())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.postLoading.set(false))
      )
      .subscribe({
        next: (p) => {
          this.post.set(p);
          this.resolveAuthor(p.authorId);
          this.loadLikeStatus();
          if (p.type === PostType.Campaign) this.loadJoinStatus();
        },
        error: (err: any) => this.handleError(err, this.postError)
      });
  }

  private loadLikeStatus(): void {
    if (!this.postId()) return;
    this.posts.getLikeStatus(this.postId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.liked.set(!!resp.liked);
          this.updatePostStats({ likeCount: Number(resp.likeCount) });
        }
      });
  }

  private loadJoinStatus(): void {
    if (!this.isAuthed() || !this.isCampaign()) return;

    this.campaigns.getJoinStatus(this.postId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.joinedCampaign.set(!!resp.joined);
          this.updatePostStats({ participantsCount: Number(resp.participantsCount) });
        }
      });
  }

  // --- Actions ---

  toggleLike(): void {
    if (!this.isAuthed()) {
      this.goToLogin(); // UX decision: Redirect if guest clicks like
      return;
    }

    if (this.likeBusy()) return;
    this.likeBusy.set(true);

    this.posts.toggleLike(this.postId())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.likeBusy.set(false))
      )
      .subscribe({
        next: (resp) => {
          const isLiked = !!resp.liked;
          this.liked.set(isLiked);
          this.updatePostStats({ likeCount: Number(resp.likeCount) });
          this.showToast(this.likeToast, isLiked ? 'Added to likes.' : 'Removed from likes.');
        }
      });
  }

  joinCampaign(): void {
    if (!this.isAuthed()) {
      this.goToLogin();
      return;
    }

    if (this.joinBusy() || this.joinedCampaign()) return;
    this.joinBusy.set(true);

    this.campaigns.join(this.postId())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.joinBusy.set(false))
      )
      .subscribe({
        next: (resp) => {
          this.joinedCampaign.set(true);
          this.updatePostStats({ participantsCount: Number(resp.participantsCount) });
          this.showToast(this.joinToast, 'Joined campaign successfully.');
        }
      });
  }

  // --- Comments ---

  refreshComments(): void {
    this.commentsPage.set(1);
    this.commentItems.set([]);
    this.fetchComments({ append: false });
  }

  loadMoreComments(): void {
    if (this.commentsLoading() || !this.canLoadMoreComments()) return;
    this.commentsPage.update(p => p + 1);
    this.fetchComments({ append: true });
  }

  private fetchComments(opts: { append: boolean }): void {
    this.commentsLoading.set(true);
    this.commentsError.set(null);

    this.comments.getForPost(this.postId(), this.commentsPage(), 20)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.commentsLoading.set(false))
      )
      .subscribe({
        next: (res) => {
          const newItems = res.items ?? [];
          if (opts.append) {
            this.commentItems.update(curr => [...curr, ...newItems]);
          } else {
            this.commentItems.set(newItems);
          }
          this.commentsTotalItems.set(res.totalItems ?? 0);

          // Batch resolve authors
          newItems.forEach(c => this.resolveAuthor(c.authorId));
        },
        error: (err: any) => this.handleError(err, this.commentsError)
      });
  }

  submitComment(): void {
    const body = this.commentDraft().trim();
    if (!body || this.creatingComment()) return;

    this.creatingComment.set(true);
    this.commentsError.set(null);

    this.comments.create(this.postId(), { body })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.creatingComment.set(false))
      )
      .subscribe({
        next: (created) => {
          this.commentDraft.set('');
          this.commentItems.update(curr => [created, ...curr]);
          this.commentsTotalItems.update(c => c + 1);
          this.resolveAuthor(created.authorId);
        },
        error: (err: any) => this.handleError(err, this.commentsError)
      });
  }

  deleteComment(id: string): void {
    if (!confirm('Are you sure?')) return;

    this.comments.delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.commentItems.update(curr => curr.filter(c => c.id !== id));
          this.commentsTotalItems.update(c => Math.max(0, c - 1));
        }
      });
  }

  // --- Author Resolution (Client-Side Join) ---

  getAuthorName(id: string): string {
    return this.authorNames()[id] || '...';
  }

  private resolveAuthor(id: string): void {
    if (!id || this.authorNames()[id]) return;

    this.authorCache.getDisplayName(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(name => {
        if (name) this.updateAuthorMap(id, name);
      });
  }

  private updateAuthorMap(id: string, name: string): void {
    this.authorNames.update(map => ({ ...map, [id]: name }));
  }

  // --- Helpers ---

  private updatePostStats(updates: Partial<PostResponse>): void {
    const p = this.post();
    if (p) this.post.set({ ...p, ...updates });
  }

  private handleError(err: any, signalToSet: any): void {
    const pd = err.error as ProblemDetails;
    signalToSet.set(pd.detail || 'Action failed.');
  }

  private showToast(toastSig: any, msg: string) {
    toastSig.set(msg);
    setTimeout(() => toastSig.set(null), 3000);
  }
}