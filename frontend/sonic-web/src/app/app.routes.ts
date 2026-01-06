import { Routes } from '@angular/router';

import { FeedPage } from './features/feed/pages/feed-page/feed-page';
import { LoginPage } from './features/auth/pages/login-page/login-page';
import { RegisterPage } from './features/auth/pages/register-page/register-page';
import { CampaignsPage } from './features/campaigns/pages/campaigns-page/campaigns-page';
import { PostDetailPage } from './features/posts/pages/post-detail-page/post-detail-page';
import { PostEditorPage } from './features/posts/pages/post-editor-page/post-editor-page';
import { ProfilePage } from './features/profile/pages/profile-page/profile-page';
import { ServerErrorPage } from './features/errors/pages/server-error-page/server-error-page';
import { NotFoundPage } from './features/errors/pages/not-found-page/not-found-page';

import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
    // Public - Home
    { path: '', component: FeedPage, pathMatch: 'full', title: 'Explore' },
    { path: 'feed', component: FeedPage, title: 'Explore' },
    { path: 'campaigns', component: CampaignsPage, title: 'Campaigns' },

    // 🔒 Auth Required Group
    // Any child route here runs the authGuard + Snack check
    {
        path: '',
        runGuardsAndResolvers: 'always',
        canActivate: [authGuard],
        children: [
            { path: 'posts/new', component: PostEditorPage, title: 'New Post' },
            { path: 'posts/:id/edit', component: PostEditorPage, title: 'Edit Post' },
            { path: 'profile', component: ProfilePage, title: 'My Profile' },
        ],
    },

    // Public Post Detail 
    // MUST remain below the Auth Group so 'posts/new' is matched first
    { path: 'posts/:id', component: PostDetailPage, title: 'Post' },

    // 🚫 Guest Group (Login/Register)
    // Logged-in users are kicked out to /feed
    {
        path: '',
        canActivate: [guestGuard],
        children: [
            { path: 'account/login', component: LoginPage, title: 'Sign In' },
            { path: 'account/register', component: RegisterPage, title: 'Create Account' },
        ],
    },

    // Errors
    { path: 'server-error', component: ServerErrorPage, title: 'Server Error' },

    // Fallback
    { path: '**', component: NotFoundPage, title: 'Page Not Found' },
];