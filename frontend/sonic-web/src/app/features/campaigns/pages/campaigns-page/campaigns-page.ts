import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PostResponse } from '../../../../shared/contracts/post/post-response';
import { AuthStateService } from '../../../../core/auth/auth-state.service';
import { CampaignsQuery, CampaignsService } from '../../../../core/campaign/campaign-service';

type ApiProblem = {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
};

@Component({
  selector: 'sonic-campaigns-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './campaigns-page.html',
  styleUrl: './campaigns-page.scss',
})
export class CampaignsPage {
  private readonly router = inject(Router);
  private readonly campaigns = inject(CampaignsService);
  private readonly authState = inject(AuthStateService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly errorCode = signal<string | null>(null);

  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly totalItems = signal(0);
  readonly items = signal<readonly PostResponse[]>([]);

  readonly canLoadMore = computed(() => this.items().length < this.totalItems());

  readonly q = signal('');
  readonly featuredOnly = signal(false);

  readonly selectedTags = signal<readonly string[]>([]);
  readonly tagDraft = signal('');

  readonly isAuthed = computed(() => this.authState.isAuthenticated());

  readonly joinBusyMap = signal<Record<string, boolean>>({});
  readonly joinedMap = signal<Record<string, boolean>>({});
  readonly joinErrorMap = signal<Record<string, string>>({});

  private readonly statusInflight = new Set<string>();

  constructor() {
    this.refresh();
  }

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

  onSearchEnter(): void {
    this.refresh();
  }

  toggleFeatured(): void {
    this.featuredOnly.set(!this.featuredOnly());
    this.refresh();
  }

  addTagFromDraft(): void {
    const t = this.tagDraft().trim();
    if (!t) return;
    if (t.includes(',')) return;

    const next = new Set(this.selectedTags());
    next.add(t);

    this.selectedTags.set(Array.from(next));
    this.tagDraft.set('');
    this.refresh();
  }

  removeTag(tag: string): void {
    this.selectedTags.set(this.selectedTags().filter(x => x !== tag));
    this.refresh();
  }

  clearFilters(): void {
    this.q.set('');
    this.featuredOnly.set(false);
    this.selectedTags.set([]);
    this.tagDraft.set('');
    this.refresh();
  }

  joinCampaign(postId: string): void {
    const id = (postId ?? '').trim();
    if (!id) return;

    if (!this.isAuthed()) {
      this.router.navigate(['/account/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    if (this.joinBusyMap()[id] === true) return;
    if (this.joinedMap()[id] === true) return;

    this.setJoinBusy(id, true);
    this.setJoinError(id, null);

    this.campaigns.join(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.setJoined(id, true);
          this.setJoinBusy(id, false);

          const nextItems = this.items().map(p => {
            if (p.id !== id) return p;
            return { ...p, participantsCount: Number(resp.participantsCount) };
          });
          this.items.set(nextItems);
        },
        error: (err: unknown) => {
          const status = this.getHttpStatus(err);
          if (status === 401 || status === 403) {
            this.setJoinBusy(id, false);
            this.setJoinError(id, 'Login required to join.');
            return;
          }

          const { message } = this.extractProblem(err);
          this.setJoinBusy(id, false);
          this.setJoinError(id, message);
        },
      });
  }

  private fetchPage(opts: { append: boolean }): void {
    this.loading.set(true);
    this.error.set(null);
    this.errorCode.set(null);

    const query: CampaignsQuery = {
      page: this.page(),
      pageSize: this.pageSize(),
      tags: this.selectedTags(),
      q: this.q(),
      featured: this.featuredOnly() ? true : null,
    };

    this.campaigns.getCampaigns(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          const incoming = result.items ?? [];
          const nextItems = opts.append ? [...this.items(), ...incoming] : incoming;

          this.items.set(nextItems);
          this.totalItems.set(result.totalItems ?? 0);
          this.loading.set(false);

          this.resolveJoinedForVisible(incoming);
        },
        error: (err: unknown) => {
          const { message, code } = this.extractProblem(err);
          this.error.set(message);
          this.errorCode.set(code);
          this.loading.set(false);
        },
      });
  }

  private resolveJoinedForVisible(posts: readonly PostResponse[]): void {
    if (!this.isAuthed()) return;

    for (const p of posts) {
      const id = (p?.id ?? '').trim();
      if (!id) continue;

      if (this.joinedMap()[id] === true) continue;
      if (this.statusInflight.has(id)) continue;

      this.statusInflight.add(id);

      this.campaigns.getJoinStatus(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (resp) => {
            if (resp.joined) this.setJoined(id, true);

            const nextItems = this.items().map(x => {
              if (x.id !== id) return x;
              return { ...x, participantsCount: Number(resp.participantsCount) };
            });
            this.items.set(nextItems);

            this.statusInflight.delete(id);
          },
          error: (err: unknown) => {
            this.statusInflight.delete(id);

            const status = this.getHttpStatus(err);
            if (status === 401 || status === 403) return;
          },
        });
    }
  }

  private setJoinBusy(postId: string, busy: boolean): void {
    const next = { ...this.joinBusyMap(), [postId]: busy };
    this.joinBusyMap.set(next);
  }

  private setJoined(postId: string, joined: boolean): void {
    const next = { ...this.joinedMap(), [postId]: joined };
    this.joinedMap.set(next);
  }

  private setJoinError(postId: string, message: string | null): void {
    const map = { ...this.joinErrorMap() };
    if (!message) {
      delete map[postId];
      this.joinErrorMap.set(map);
      return;
    }
    map[postId] = message;
    this.joinErrorMap.set(map);
  }

  private extractProblem(err: unknown): { message: string; code: string | null } {
    const anyErr = err as any;
    const problem: ApiProblem | undefined = anyErr?.error;

    const message =
      (typeof problem?.detail === 'string' && problem.detail.trim()) ||
      'Request failed.';

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
