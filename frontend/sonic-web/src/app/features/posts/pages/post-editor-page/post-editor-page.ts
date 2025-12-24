import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs/operators';

import { PostType } from '../../../../shared/contracts/post/post-type';
import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { CreatePostRequest } from '../../../../shared/contracts/post/create-post-request';
import { UpdatePostRequest } from '../../../../shared/contracts/post/update-post-request';
import { PostsService } from '../../../../core/posts/post-service';

type ApiProblem = {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
};

@Component({
  selector: 'sonic-post-editor-page',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './post-editor-page.html',
  styleUrl: './post-editor-page.scss',
})
export class PostEditorPage {
  private readonly posts = inject(PostsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  // ---- mode
  readonly postId = signal<string | null>(null);
  readonly isEdit = computed(() => !!this.postId());

  // ---- UI state
  readonly loading = signal(false);
  readonly saving = signal(false);

  // API-only errors
  readonly apiError = signal<string | null>(null);
  readonly apiErrorCode = signal<string | null>(null);

  // UI validation display
  readonly submitted = signal(false);

  // ---- tags
  readonly selectedTags = signal<readonly string[]>([]);
  readonly tagDraft = signal('');

  // ---- type (edit mode needs a stable type)
  private readonly originalType = signal<PostType>(PostType.Experience);

  readonly typeOptions: readonly PostType[] = [
    PostType.Experience,
    PostType.Idea,
    PostType.ModelGuide,
    PostType.Course,
    PostType.News,
    PostType.Campaign,
  ];

  // ---- form (TITLE REQUIRED)
  readonly form = this.fb.nonNullable.group({
    type: this.fb.nonNullable.control<PostType>(PostType.Experience, {
      validators: [Validators.required],
    }),
    title: this.fb.nonNullable.control<string>('', {
      validators: [Validators.required],
    }),
    body: this.fb.nonNullable.control<string>('', {
      validators: [Validators.required],
    }),
    externalLink: this.fb.nonNullable.control<string>(''),
    campaignGoal: this.fb.nonNullable.control<string>(''),
  });

  // ---- bridge Reactive Forms -> Signals (this is what fixes the stuck disabled button)
  private readonly formStatus = toSignal(
    this.form.statusChanges.pipe(startWith(this.form.status)),
    { initialValue: this.form.status }
  );

  private readonly titleValue = toSignal(
    this.form.controls.title.valueChanges.pipe(startWith(this.form.controls.title.value)),
    { initialValue: this.form.controls.title.value }
  );

  private readonly bodyValue = toSignal(
    this.form.controls.body.valueChanges.pipe(startWith(this.form.controls.body.value)),
    { initialValue: this.form.controls.body.value }
  );

  private readonly typeValue = toSignal(
    this.form.controls.type.valueChanges.pipe(startWith(this.form.controls.type.value)),
    { initialValue: this.form.controls.type.value }
  );

  // effective type: create uses live select, edit uses original type
  readonly effectiveType = computed(() => (this.isEdit() ? this.originalType() : this.typeValue()));
  readonly isCampaign = computed(() => this.effectiveType() === PostType.Campaign);

  readonly titleError = computed(() => {
    const show = this.submitted() || this.form.controls.title.touched;
    if (!show) return null;

    const title = (this.titleValue() ?? '').trim();
    if (!title) return 'Title is required.';
    return null;
  });

  readonly bodyError = computed(() => {
    const show = this.submitted() || this.form.controls.body.touched;
    if (!show) return null;

    const body = (this.bodyValue() ?? '').trim();
    if (!body) return 'Body is required.';
    return null;
  });

  readonly canSubmit = computed(() => {
    if (this.loading() || this.saving()) return false;

    // formStatus is now reactive (signals get updated)
    if (this.formStatus() !== 'VALID') return false;

    // trim-based guards (VALID can be true with whitespace)
    const title = (this.titleValue() ?? '').trim();
    const body = (this.bodyValue() ?? '').trim();

    return title.length > 0 && body.length > 0;
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    this.postId.set(id);

    // if user changes type away from Campaign => wipe campaignGoal
    this.form.controls.type.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((t) => {
        if (t !== PostType.Campaign) {
          this.form.controls.campaignGoal.setValue('');
        }
      });

    if (id) {
      this.loadForEdit(id);
    }
  }

  // --------------------
  // actions
  // --------------------
  submit(): void {
    this.submitted.set(true);
    this.apiError.set(null);
    this.apiErrorCode.set(null);

    // UI validation first
    if (!this.canSubmit()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const title = (this.titleValue() ?? '').trim();
    const body = (this.bodyValue() ?? '').trim();

    const externalLink = this.nullIfBlank(this.form.controls.externalLink.value);

    // CampaignGoal ONLY when Campaign
    const campaignGoal = this.isCampaign()
      ? this.nullIfBlank(this.form.controls.campaignGoal.value)
      : null;

    const tags = Array.from(new Set(this.selectedTags().map(t => t.trim()).filter(Boolean)));

    if (this.isEdit()) {
      const id = this.postId()!;
      const req: UpdatePostRequest = {
        title,
        body,
        tags: [...tags],
        externalLink,
        campaignGoal,
      };

      this.posts.update(id, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (updated) => this.onSaved(updated),
          error: (err: unknown) => this.onApiError(err),
        });

      return;
    }

    const req: CreatePostRequest = {
      type: this.typeValue(),
      title,
      body,
      tags: [...tags],
      externalLink,
      campaignGoal,
    };

    this.posts.create(req)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => this.onSaved(created),
        error: (err: unknown) => this.onApiError(err),
      });
  }

  cancel(): void {
    const id = this.postId();
    if (id) {
      this.router.navigate(['/posts', id]);
      return;
    }
    this.router.navigate(['/feed']);
  }

  // ---- tags (one-by-one, no commas)
  addTagFromDraft(): void {
    const t = this.tagDraft().trim();
    if (!t) return;

    // enforce one tag at a time
    if (t.includes(',')) return;

    const next = new Set(this.selectedTags());
    next.add(t);

    this.selectedTags.set(Array.from(next));
    this.tagDraft.set('');
  }

  removeTag(tag: string): void {
    this.selectedTags.set(this.selectedTags().filter(x => x !== tag));
  }

  // --------------------
  // internal
  // --------------------
  private loadForEdit(id: string): void {
    this.loading.set(true);
    this.apiError.set(null);
    this.apiErrorCode.set(null);

    this.posts.getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (post) => {
          this.applyPostToForm(post);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.apiError.set(message);
          this.apiErrorCode.set(code);
          this.loading.set(false);
        },
      });
  }

  private applyPostToForm(post: PostResponse): void {
    this.originalType.set(post.type);

    this.form.patchValue({
      type: post.type,
      title: post.title ?? '',
      body: post.body ?? '',
      externalLink: post.externalLink ?? '',
      campaignGoal: post.campaignGoal ?? '',
    });

    this.selectedTags.set(Array.isArray(post.tags) ? post.tags : []);

    // we still disable type in edit mode
    this.form.controls.type.disable({ emitEvent: false });

    // if not campaign => wipe it so UI + payload stays clean
    if (post.type !== PostType.Campaign) {
      this.form.controls.campaignGoal.setValue('');
    }
  }

  private onSaved(post: PostResponse): void {
    this.saving.set(false);
    this.router.navigate(['/posts', post.id]);
  }

  private onApiError(err: unknown): void {
    const { message, code } = this.extractProblem(err);
    this.apiError.set(message);
    this.apiErrorCode.set(code);
    this.saving.set(false);
  }

  private extractProblem(err: unknown): { message: string; code: string | null } {
    const anyErr = err as any;
    const problem: ApiProblem | undefined = anyErr?.error;

    const message =
      (typeof problem?.detail === 'string' && problem.detail.trim()) ||
      'Request failed.';

    const code =
      (typeof problem?.code === 'string' && problem.code.trim()) ||
      null;

    return { message, code };
  }

  private nullIfBlank(value: string): string | null {
    const v = (value ?? '').trim();
    return v.length ? v : null;
  }
}
