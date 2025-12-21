import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, filter, map } from 'rxjs/operators';
import { MatSnackBar } from '@angular/material/snack-bar';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { CommentResponse } from '../../../../shared/contracts/comment/create-comment.response';
import { CreateCommentRequest } from '../../../../shared/contracts/comment/create-comment-request';
import { PostsService } from '../../../../core/posts/post-service';
import { CommentsService } from '../../../../core/comments/comment-service';
import { UsersService } from '../../../../core/users/user-service';
import { AuthorDisplayNameCache } from '../../../../core/users/author-name-cache';

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
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  private readonly posts = inject(PostsService);
  private readonly commentsApi = inject(CommentsService);
  private readonly users = inject(UsersService);
  private readonly authorCache = inject(AuthorDisplayNameCache);

  readonly postId = signal<string | null>(null);

  readonly currentUserId = signal<string | null>(null);
  readonly currentUserRole = signal<string | null>(null);
  readonly isAdmin = computed(() => this.currentUserRole() === 'Admin');

  readonly loadingPost = signal(false);
  readonly postError = signal<string | null>(null);
  readonly postErrorCode = signal<string | null>(null);
  readonly post = signal<PostResponse | null>(null);

  readonly authorDisplayName = signal<string | null>(null);

  readonly likeBusy = signal(false);
  readonly likeCount = signal(0);
  readonly liked = signal<boolean | null>(null);

  readonly loadingComments = signal(false);
  readonly commentsError = signal<string | null>(null);
  readonly commentsErrorCode = signal<string | null>(null);

  readonly commentsPage = signal(1);
  readonly commentsPageSize = signal(20);
  readonly commentsTotalItems = signal(0);
  readonly comments = signal<readonly CommentResponse[]>([]);
  readonly canLoadMoreComments = computed(() => this.comments().length < this.commentsTotalItems());

  readonly commentDraft = signal('');
  readonly commentBusy = signal(false);

  constructor() {
    this.users.getMe()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (me) => {
          this.currentUserId.set(me.id);
          this.currentUserRole.set(me.role);
        },
        error: () => {
          this.currentUserId.set(null);
          this.currentUserRole.set(null);
        },
      });

    this.route.paramMap
      .pipe(
        map(pm => (pm.get('id') ?? '').trim()),
        filter(id => id.length > 0),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (id) => {
          this.postId.set(id);
          this.loadPost(id);
          this.resetAndLoadComments();
        },
      });
  }

  toggleLike(): void {
    const id = this.postId();
    if (!id || this.likeBusy()) return;

    this.likeBusy.set(true);

    this.posts.toggleLike(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.likeCount.set(resp.likeCount);
          this.liked.set(resp.liked);
          this.likeBusy.set(false);
        },
        error: (err: unknown) => {
          this.handleAuthErrors(err, 'Please sign in to like posts.');
          const { message, code } = this.extractProblem(err);
          this.postError.set(message);
          this.postErrorCode.set(code);
          this.likeBusy.set(false);
        },
      });
  }

  addComment(): void {
    const postId = this.postId();
    const body = this.commentDraft().trim();
    if (!postId || this.commentBusy()) return;
    if (!body) return;

    this.commentBusy.set(true);
    this.commentsError.set(null);
    this.commentsErrorCode.set(null);

    const req: CreateCommentRequest = { body };

    this.commentsApi.create(postId, req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.commentDraft.set('');
          this.commentBusy.set(false);
          this.resetAndLoadComments();
        },
        error: (err: unknown) => {
          this.handleAuthErrors(err, 'Please sign in to comment.');
          const { message, code } = this.extractProblem(err);
          this.commentsError.set(message);
          this.commentsErrorCode.set(code);
          this.commentBusy.set(false);
        },
      });
  }

  loadMoreComments(): void {
    if (this.loadingComments() || !this.canLoadMoreComments()) return;
    this.commentsPage.set(this.commentsPage() + 1);
    this.loadComments({ append: true });
  }

  deleteComment(commentId: string): void {
    const id = (commentId ?? '').trim();
    if (!id) return;

    this.commentsApi.delete(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.comments.set(this.comments().filter(c => c.id !== id));
          this.commentsTotalItems.set(Math.max(0, this.commentsTotalItems() - 1));
        },
        error: (err: unknown) => {
          this.handleAuthErrors(err, 'Please sign in to manage comments.');
          const { message, code } = this.extractProblem(err);
          this.commentsError.set(message);
          this.commentsErrorCode.set(code);
        },
      });
  }

  canDeleteComment(c: CommentResponse): boolean {
    const meId = this.currentUserId();
    if (!meId) return false;
    return this.isAdmin() || c.authorId === meId;
  }

  private loadPost(id: string): void {
    this.loadingPost.set(true);
    this.postError.set(null);
    this.postErrorCode.set(null);
    this.post.set(null);
    this.authorDisplayName.set(null);
    this.liked.set(null);

    this.posts.getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (p) => {
          this.post.set(p);
          this.likeCount.set(p.likeCount);

          this.authorCache.getDisplayName(p.authorId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (name) => this.authorDisplayName.set(name),
            });

          this.loadingPost.set(false);
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.postError.set(message);
          this.postErrorCode.set(code);
          this.loadingPost.set(false);
        },
      });
  }

  private resetAndLoadComments(): void {
    this.commentsPage.set(1);
    this.comments.set([]);
    this.commentsTotalItems.set(0);
    this.loadComments({ append: false });
  }

  private loadComments(opts: { append: boolean }): void {
    const postId = this.postId();
    if (!postId) return;

    this.loadingComments.set(true);
    this.commentsError.set(null);
    this.commentsErrorCode.set(null);

    this.commentsApi.getForPost(postId, this.commentsPage(), this.commentsPageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          for (const c of res.items) {
            if (c.authorDisplayName) {
              this.authorCache.seed(c.authorId, c.authorDisplayName);
            }
          }

          const nextItems = opts.append ? [...this.comments(), ...res.items] : res.items;
          this.comments.set(nextItems);
          this.commentsTotalItems.set(res.totalItems);
          this.loadingComments.set(false);
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.commentsError.set(message);
          this.commentsErrorCode.set(code);
          this.loadingComments.set(false);
        },
      });
  }

  private handleAuthErrors(err: unknown, unauthorizedMsg: string): void {
    const status = (err as any)?.status;
    if (status === 401) {
      this.snack.open(unauthorizedMsg, 'OK', { duration: 3500 });
      this.router.navigate(['/account/login'], {
        queryParams: { returnUrl: this.router.url, reason: 'auth-required' },
      });
    } else if (status === 403) {
      this.snack.open('You do not have permission to do this.', 'OK', { duration: 3500 });
    }
  }

  private extractProblem(err: unknown): { message: string; code: string | null } {
    const anyErr = err as any;

    const status: number | null =
      typeof anyErr?.status === 'number' ? anyErr.status : null;

    const problem: ApiProblem | string | undefined = anyErr?.error;

    let message = 'Request failed.';
    let code: string | null = null;

    if (typeof problem === 'string' && problem.trim()) {
      message = problem.trim();
    } else if (problem && typeof problem === 'object') {
      message =
        (typeof problem.detail === 'string' && problem.detail.trim()) ||
        (typeof problem.title === 'string' && problem.title.trim()) ||
        message;

      code =
        (typeof problem.code === 'string' && problem.code.trim()) ||
        null;
    }

    if ((message === 'Request failed.' || !message.trim()) && status !== null) {
      if (status === 401) message = 'Unauthorized. Please log in and try again.';
      else if (status === 403) message = 'Forbidden. You do not have permission.';
      else if (status === 0) message = 'Network/CORS error: cannot reach API.';
    }

    if (!code && status !== null) code = String(status);

    return { message, code };
  }
}
