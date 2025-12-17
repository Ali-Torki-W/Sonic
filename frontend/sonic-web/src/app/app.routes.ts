import { Routes } from '@angular/router';

import { HomePage } from './features/home/pages/home-page/home-page';
import { FeedPage } from './features/feed/pages/feed-page/feed-page';
import { LoginPage } from './features/auth/pages/login-page/login-page';
import { RegisterPage } from './features/auth/pages/register-page/register-page';
import { CampaignsPage } from './features/campaigns/pages/campaigns-page/campaigns-page';
import { PostDetailPage } from './features/posts/pages/post-detail-page/post-detail-page';
import { PostEditorPage } from './features/posts/pages/post-editor-page/post-editor-page';
import { ProfilePage } from './features/profile/pages/profile-page/profile-page';
import { ServerErrorPage } from './features/misc/pages/server-error-page/server-error-page';
import { NotFoundPage } from './features/misc/pages/not-found-page/not-found-page';

import { authGuard } from './core/guards/auth-guard';
import { authLoggedInGuard } from './core/guards/auth-logged-in-guard';

export const routes: Routes = [
    // Landing / Home
    { path: '', pathMatch: 'full', component: HomePage },
    { path: 'home', component: HomePage },

    // Public
    { path: 'feed', component: FeedPage },
    { path: 'campaigns', component: CampaignsPage },
    { path: 'posts/:id', component: PostDetailPage },

    // Auth required
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [authGuard],
        children: [
            { path: 'posts/new', component: PostEditorPage },
            { path: 'posts/:id/edit', component: PostEditorPage },
            { path: 'profile', component: ProfilePage },
        ],
    },

    // Logged-in users shouldn't hit login/register
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [authLoggedInGuard],
        children: [
            { path: 'account/login', component: LoginPage },
            { path: 'account/register', component: RegisterPage },
        ],
    },

    // Errors
    { path: 'server-error', component: ServerErrorPage },

    // Fallback
    { path: '**', component: NotFoundPage },
];
