import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { CommentResponse } from '../../../../shared/contracts/comment/create-comment.response';
import { CreateCommentRequest } from '../../../../shared/contracts/comment/create-comment-request';
import { PostType } from '../../../../shared/contracts/post/post-type';

import { PostsService } from '../../../../core/posts/post-service';
import { CommentsService } from '../../../../core/comments/comment-service';
import { AuthorDisplayNameCache } from '../../../../core/users/author-name-cache';
import { UsersService } from '../../../../core/users/user-service';
import { AuthStateService } from '../../../../core/auth/auth-state.service';

type ApiProblem = {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
};

@Component({
  selector: 'sonic-post-detail-page',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './post-detail-page.html',
  styleUrl: './post-detail-page.scss',
})
export class PostDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly posts = inject(PostsService);
  private readonly comments = inject(CommentsService);
  private readonly authorNames = inject(AuthorDisplayNameCache);
  private readonly users = inject(UsersService);
  private readonly authState = inject(AuthStateService);
  private readonly destroyRef = inject(DestroyRef);

  // ---- ids
  readonly postId = signal<string>('');

  // ---- current user (for canEdit)
  readonly meId = signal<string | null>(null);
  readonly meRole = signal<string | null>(null);
  readonly meLoading = signal(false);

  // ---- post
  readonly postLoading = signal(false);
  readonly postError = signal<string | null>(null);
  readonly postErrorCode = signal<string | null>(null);
  readonly post = signal<PostResponse | null>(null);

  // ---- like (UX)
  readonly likeBusy = signal(false);
  readonly liked = signal(false);
  readonly likeToast = signal<string | null>(null);
  readonly likeError = signal<string | null>(null);
  readonly likeErrorCode = signal<string | null>(null);
  readonly likeNeedsLogin = signal(false);

  // ---- author display names (resolved -> stored as strings only)
  readonly authorNameMap = signal<Record<string, string>>({});

  // ---- comments (paged)
  readonly commentsLoading = signal(false);
  readonly commentsError = signal<string | null>(null);
  readonly commentsErrorCode = signal<string | null>(null);

  readonly commentsPage = signal(1);
  readonly commentsPageSize = signal(20);
  readonly commentsTotalItems = signal(0);
  readonly commentItems = signal<readonly CommentResponse[]>([]);

  readonly canLoadMoreComments = computed(
    () => this.commentItems().length < this.commentsTotalItems()
  );

  // ---- create comment
  readonly commentDraft = signal('');
  readonly creatingComment = signal(false);

  // ---- derived
  readonly isCampaign = computed(() => this.post()?.type === PostType.Campaign);

  readonly canEditPost = computed(() => {
    const p = this.post();
    if (!p) return false;

    if (!this.authState.isAuthenticated()) return false;

    const meId = this.meId();
    const role = (this.meRole() ?? '').trim().toLowerCase();

    const isAdmin = role === 'admin';
    const isAuthor = !!meId && meId === p.authorId;

    return isAuthor || isAdmin;
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/not-found']);
      return;
    }

    this.postId.set(id);

    // load current user only if we have a valid session
    this.loadMeIfAuthenticated();

    this.loadPost();
    this.refreshComments();
  }

  // --------------------
  // navigation
  // --------------------
  backToFeed(): void {
    this.router.navigate(['/feed']);
  }

  goToLogin(): void {
    this.router.navigate(['/account/login'], {
      queryParams: { returnUrl: this.router.url },
    });
  }

  goToEdit(): void {
    const p = this.post();
    if (!p) return;
    this.router.navigate([`/posts/${encodeURIComponent(p.id)}/edit`]);
  }

  // --------------------
  // current user
  // --------------------
  private loadMeIfAuthenticated(): void {
    if (!this.authState.isAuthenticated()) return;

    this.meLoading.set(true);

    this.users
      .getMe()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (me) => {
          this.meId.set(me.id);
          this.meRole.set(me.role);
          this.meLoading.set(false);

          // optional: seed name cache for "me"
          if (me.displayName?.trim()) {
            this.setAuthorName(me.id, me.displayName.trim());
          }
        },
        error: () => {
          // do not block the page if /users/me fails
          this.meId.set(null);
          this.meRole.set(null);
          this.meLoading.set(false);
        },
      });
  }

  // --------------------
  // post
  // --------------------
  private loadPost(): void {
    const id = this.postId();

    this.postLoading.set(true);
    this.postError.set(null);
    this.postErrorCode.set(null);

    this.posts
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (p) => {
          this.post.set(p);
          this.postLoading.set(false);

          this.resolveAuthorName(p.authorId);

          // reload-safe: ask backend if CURRENT user likes it
          this.loadLikeStatus();
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.postError.set(message);
          this.postErrorCode.set(code);
          this.postLoading.set(false);
        },
      });
  }

  private loadLikeStatus(): void {
    const id = this.postId();
    if (!id) return;

    this.posts
      .getLikeStatus(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.liked.set(!!resp.liked);

          const p = this.post();
          if (p) this.post.set({ ...p, likeCount: Number(resp.likeCount) });
        },
        error: (err: unknown) => {
          const status = this.getHttpStatus(err);
          if (status === 401 || status === 403) {
            this.liked.set(false);
          }
        },
      });
  }

  toggleLike(): void {
    const p = this.post();
    if (!p || this.likeBusy()) return;

    this.likeBusy.set(true);
    this.likeToast.set(null);
    this.likeError.set(null);
    this.likeErrorCode.set(null);
    this.likeNeedsLogin.set(false);

    this.posts
      .toggleLike(p.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          const nowLiked = !!resp.liked;

          this.liked.set(nowLiked);
          this.post.set({ ...p, likeCount: Number(resp.likeCount) });

          this.likeBusy.set(false);

          this.likeToast.set(nowLiked ? 'Added to likes.' : 'Removed from likes.');
          window.setTimeout(() => this.likeToast.set(null), 1800);
        },
        error: (err: unknown) => {
          const status = this.getHttpStatus(err);

          if (status === 401 || status === 403) {
            const { message, code } = this.extractProblem(err);
            this.likeError.set(message || 'Please login to like posts.');
            this.likeErrorCode.set(code ?? 'auth.unauthorized');
            this.likeNeedsLogin.set(true);
            this.likeBusy.set(false);
            return;
          }

          const { message, code } = this.extractProblem(err);
          this.likeError.set(message);
          this.likeErrorCode.set(code);
          this.likeBusy.set(false);
        },
      });
  }

  // --------------------
  // comments (paging)
  // --------------------
  refreshComments(): void {
    this.commentsPage.set(1);
    this.commentItems.set([]);
    this.fetchComments({ append: false });
  }

  loadMoreComments(): void {
    if (this.commentsLoading() || !this.canLoadMoreComments()) return;

    this.commentsPage.set(this.commentsPage() + 1);
    this.fetchComments({ append: true });
  }

  private fetchComments(opts: { append: boolean }): void {
    const postId = this.postId();

    this.commentsLoading.set(true);
    this.commentsError.set(null);
    this.commentsErrorCode.set(null);

    this.comments
      .getForPost(postId, this.commentsPage(), this.commentsPageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          const incoming = result.items ?? [];
          const nextItems = opts.append ? [...this.commentItems(), ...incoming] : incoming;

          this.commentItems.set(nextItems);
          this.commentsTotalItems.set(result.totalItems ?? 0);
          this.commentsLoading.set(false);

          for (const c of incoming) {
            if (c.authorDisplayName && c.authorDisplayName.trim()) {
              this.setAuthorName(c.authorId, c.authorDisplayName.trim());
            } else {
              this.resolveAuthorName(c.authorId);
            }
          }
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.commentsError.set(message);
          this.commentsErrorCode.set(code);
          this.commentsLoading.set(false);
        },
      });
  }

  submitComment(): void {
    const postId = this.postId();
    const body = this.commentDraft().trim();
    if (!body || this.creatingComment()) return;

    this.creatingComment.set(true);
    this.commentsError.set(null);
    this.commentsErrorCode.set(null);

    const req: CreateCommentRequest = { body };

    this.comments
      .create(postId, req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.commentDraft.set('');
          this.creatingComment.set(false);

          this.commentItems.set([created, ...this.commentItems()]);
          this.commentsTotalItems.set(this.commentsTotalItems() + 1);

          if (created.authorDisplayName && created.authorDisplayName.trim()) {
            this.setAuthorName(created.authorId, created.authorDisplayName.trim());
          } else {
            this.resolveAuthorName(created.authorId);
          }
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.commentsError.set(message);
          this.commentsErrorCode.set(code);
          this.creatingComment.set(false);
        },
      });
  }

  deleteComment(commentId: string): void {
    const id = (commentId ?? '').trim();
    if (!id) return;

    this.commentsError.set(null);
    this.commentsErrorCode.set(null);

    this.comments
      .delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          const next = this.commentItems().filter((c) => c.id !== id);
          this.commentItems.set(next);
          this.commentsTotalItems.set(Math.max(0, this.commentsTotalItems() - 1));
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.commentsError.set(message);
          this.commentsErrorCode.set(code);
        },
      });
  }

  // --------------------
  // author display helpers
  // --------------------
  displayAuthorName(authorId: string): string {
    const id = (authorId ?? '').trim();
    if (!id) return 'Unknown';

    const map = this.authorNameMap();
    return map[id] ?? this.shortId(id);
  }

  private resolveAuthorName(authorId: string): void {
    const id = (authorId ?? '').trim();
    if (!id) return;

    if (this.authorNameMap()[id]) return;

    this.authorNames
      .getDisplayName(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (nameOrNull) => {
          const name = (nameOrNull ?? '').trim();
          if (!name) return;
          this.setAuthorName(id, name);
        },
      });
  }

  private setAuthorName(authorId: string, displayName: string): void {
    const id = (authorId ?? '').trim();
    const name = (displayName ?? '').trim();
    if (!id || !name) return;

    const next = { ...this.authorNameMap(), [id]: name };
    this.authorNameMap.set(next);
  }

  private shortId(id: string): string {
    return id.length > 10 ? `${id.slice(0, 6)}…${id.slice(-4)}` : id;
  }

  // --------------------
  // errors
  // --------------------
  private extractProblem(err: unknown): { message: string; code: string | null } {
    const anyErr = err as any;
    const problem: ApiProblem | undefined = anyErr?.error;

    const message =
      (typeof problem?.detail === 'string' && problem.detail.trim()) || 'Request failed.';

    const code = (typeof problem?.code === 'string' && problem.code.trim()) || null;

    return { message, code };
  }

  private getHttpStatus(err: unknown): number | null {
    const anyErr = err as any;
    if (typeof anyErr?.status === 'number') return anyErr.status;

    const problem: ApiProblem | undefined = anyErr?.error;
    if (typeof problem?.status === 'number') return problem.status;

    return null;
  }
}
