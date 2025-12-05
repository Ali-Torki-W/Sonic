Sonic – MVP Requirements Specification (v1.0)
1. Purpose

The purpose of Sonic is to provide an AI-focused platform where users share practical experiences, ideas, and resources about using AI in real-world jobs. The primary goal of the MVP is to deliver a production-deployable, visually attractive web application that:

Demonstrates solid full-stack engineering skills (.NET, Angular, MongoDB).

Is small and realistic enough to be implemented by a single developer in ~1 month.

Can be used as a portfolio / interview showcase.

This document defines the functional and non-functional requirements for the Sonic MVP.

2. Product Overview

Sonic is an online platform where users can:

Share experiences and ideas on how AI is used in different jobs.

Discover AI model guides (which models are useful for which tasks).

Browse AI-related learning courses and AI news.

Create and participate in campaigns (collaborative calls-to-action around specific AI challenges or topics).

All these are implemented under a unified “content/post” model, differentiated by type.

3. Scope (MVP)
3.1 In Scope (MVP)

Web application (SPA) with:

User registration, login, and basic profile management.

Content creation and browsing for multiple content types:

Experiences / Ideas

Model guides

Courses

News

Campaigns

Feed & filtering by content type, tags, and search term.

Likes/upvotes and comments on content.

Campaign participation (join a campaign).

Basic admin moderation (minimal).

Production-capable architecture with .NET backend, Angular frontend, and MongoDB.

Basic deployment pipeline and documentation.

3.2 Out of Scope (Post-MVP)

These are explicitly not required for the 1-month MVP:

Advanced analytics, dashboards, or reports.

Complex role-based access control beyond User/Admin.

Rich WYSIWYG editors with media uploads.

Notifications (email, in-app, push).

Social graph (follow/followers).

Multi-language UI (MVP is English-only).

Mobile apps (native).

4. Definitions

User: An authenticated person using the platform.

Anonymous User / Visitor: A non-authenticated user browsing public content.

Admin: User with elevated privileges for moderation.

Post / Content: A generic content entity representing one of several types.

Content Type:

Experience/Idea: Real-world usage of AI, lessons, tips.

ModelGuide: Description of a specific AI model or group of models and their best-fit tasks.

Course: Link or description of AI-related learning materials.

News: AI-related news item.

Campaign: Collaborative action or challenge where users can join.

5. User Roles & Permissions
5.1 Roles

Anonymous Visitor

Can view public posts, feeds, and comments.

Cannot create, edit, like, comment, or join campaigns.

Authenticated User

All anonymous abilities.

Create, edit, and delete their own posts.

Like/unlike posts.

Comment on posts (and delete own comments).

Join campaigns.

Edit their own profile.

Admin

All authenticated user abilities.

View list of users.

Soft-delete any post or comment.

Mark posts as “featured”.

Optionally block/unblock users (if implemented later; can be post-MVP).

6. Functional Requirements
6.1 Authentication & Authorization

FR-1 – User Registration

The system shall allow a new user to register with:

Email (unique)

Password

Display name

The system shall validate password strength according to defined rules.

The system shall prevent duplicate email registrations.

FR-2 – User Login

The system shall allow a user to log in using email and password.

On successful login, the system shall issue a JWT access token (and optionally a refresh token).

On login failure (invalid credentials), a generic error shall be returned (no user existence leak).

FR-3 – Logout

The system shall provide a mechanism for the client to log out (e.g. remove tokens on client; optional server-side invalidation if refresh tokens are used).

FR-4 – Authorization

The system shall restrict endpoints to:

Public: read-only endpoints for listing and viewing posts and comments.

Authenticated: actions like create/edit/delete own posts, comments, likes, join campaigns.

Admin: moderation endpoints (delete any post/comment, mark featured).

The system shall validate the user’s role for each protected operation.

6.2 User Profile Management

FR-5 – View Own Profile

The system shall allow an authenticated user to view their profile, including:

Display name

Email (non-editable for MVP)

Bio

Job role/title

AI-related interests (tags)

Avatar URL (optional)

FR-6 – Edit Profile

The system shall allow an authenticated user to update:

Display name

Bio

Job role/title

AI-related interest tags

Avatar URL

FR-7 – Public Profile View (MVP Lite)

The system shall allow viewing a public profile summary when clicking on a username (limited to display name, avatar, and brief stats like number of posts).

6.3 Content Management (Posts)

All content is stored under a single Post entity with a type field.

FR-8 – Create Post

The system shall allow authenticated users to create a new post with:

Title (required)

Body/content (required)

Type (one of: Experience/Idea, ModelGuide, Course, News, Campaign)

Tags (0..N tags)

Optional external link (for Courses and News)

The system shall default the post’s visibility to public.

FR-9 – Edit Post

The system shall allow a post’s author (or an admin) to edit:

Title

Body/content

Tags

External link (if applicable)

The post type should generally remain unchanged for MVP (to avoid complexity).

FR-10 – Delete Post

The system shall allow the author to delete their own post.

The system shall allow admins to delete (soft-delete) any post.

Deleted posts shall not appear in public listings.

FR-11 – View Post Details

The system shall allow any user (including anonymous) to view a post’s details:

Title, body, type, tags, author (display name), created/updated timestamps.

Like count, comment count.

For Campaigns: extra campaign-related fields (see 6.5).

6.4 Feed & Discovery

FR-12 – Global Feed

The system shall provide a default feed endpoint that returns a paginated list of posts, ordered by most recent by default.

FR-13 – Filter by Content Type

The feed shall support filtering by content type, e.g.:

Experiences/Ideas

Model guides

Courses

News

Campaigns

FR-14 – Tag Filtering

The feed shall support filtering by one or more tags.

If multiple tags are provided, the behavior for MVP may be:

"OR" logic (posts matching any tag), or

"AND" logic (posts matching all tags)
(Pick one and document it; keep consistent.)

FR-15 – Search

The system shall allow searching posts by a text query against:

Title (primary)

Body (optional, if feasible for MVP)

Search results shall be paginated.

FR-16 – Sorting

The system shall support at least:

Sort by newest (default).

If time permits, add optional:

Sort by most liked / trending (using like count).

6.5 Campaigns

Campaigns are specific posts with type = Campaign.

FR-17 – Create Campaign

The system shall allow an authenticated user to create a campaign post with:

Title

Description (body)

Tags

Optional fields such as:

Goal statement (short text)

Campaigns are visible in the main feed and on a separate Campaigns view.

FR-18 – Join / Participate in Campaign

The system shall allow authenticated users to “join” a campaign.

A user cannot join the same campaign more than once.

The campaign detail view shall display:

Number of participants.

(Optionally) a list of participants’ display names.

FR-19 – Campaign Listing

The system shall provide a view/filter to show only Campaigns, with pagination.

6.6 Comments & Interactions

FR-20 – Comments

The system shall allow authenticated users to add comments to any post.

A comment includes:

Text content

Author

Timestamp

The system shall display comments under a post in chronological order (newest last or newest first; pick one and be consistent).

FR-21 – Edit/Delete Own Comments

The system shall allow a user to delete their own comment.

Editing comments is optional for MVP; if implemented:

The comment shall show that it has been edited.

FR-22 – Admin Comment Moderation

Admins shall be able to delete (soft-delete) any comment.

FR-23 – Likes / Upvotes

The system shall allow authenticated users to like/unlike a post.

Each user may only like a specific post once.

The like count shall be displayed in the feed and post detail view.

6.7 Admin & Moderation (MVP Level)

FR-24 – Admin Flagging & Deletion

Admins shall be able to:

Soft-delete posts.

Soft-delete comments.

FR-25 – Featured Content

Admins shall be able to mark a post as featured.

The system shall provide a way to display featured posts in a special section on the home/feed page.

FR-26 – Admin Access

Admin functions shall be accessible only to users with role = Admin.

(More advanced moderation like blocking users is post-MVP.)

6.8 System & Configuration

FR-27 – Tag Management (MVP Lite)

Users can freely add tags as plain text when creating posts.

The system may suggest existing tags (as autocomplete) if time allows, but this is not mandatory.

FR-28 – Public Landing Experience

When an anonymous user visits the site, they should see:

A global feed or a landing view highlighting featured content and basic explanation of Sonic.

Clear calls to action to register or log in.

7. Non-Functional Requirements
7.1 Performance

The system shall respond to typical API requests (e.g. fetching feed, viewing a post) in under 1 second under normal load on the target hosting.

Pagination shall be used for lists (feed, comments) to avoid large payloads.

7.2 Security

Passwords shall be stored using a secure hashing algorithm.

All authenticated endpoints shall require a valid JWT.

Authorization checks shall be enforced server-side (not only on the frontend).

Input validation shall be performed on all public endpoints to reduce risk of injection or malformed data.

7.3 Availability & Reliability (MVP Level)

Sonic shall be deployable on a standard cloud environment (e.g. single region, single instance).

There is no strict SLA for MVP, but the system should handle restarts without data loss (MongoDB used as persistent storage).

7.4 Usability & UX

The UI shall be responsive for desktop and mobile screen widths.

Main flows (register, login, create post, browse feed, join campaign) should be navigable in a few obvious steps.

Error and success states shall be clearly indicated (e.g. toast messages, inline validation errors).

7.5 Internationalization

MVP: English-only UI and content.

Design should not block adding localization later (e.g. avoid space-dependent UI hacks).

7.6 Logging & Monitoring

The backend shall log:

Errors (with stack traces, not exposed to clients).

Key events (user registration, login failures, post creation).

A basic health check endpoint shall exist for liveness checks.

8. Technical Constraints & Stack

Backend

.NET 10 (ASP.NET Core).

RESTful API.

JWT-based authentication.

Frontend

Angular 20 (standalone components, modern Angular patterns).

SPA consuming the backend API.

Database

MongoDB (cloud-hosted, e.g. MongoDB Atlas).

Deployment

Containerized or standard app service deployment.

At least one environment:

Production (public URL).

Optional: separate dev/staging environment.

9. Data Model (High-Level, MVP)

This is not the DB schema, just core entities.

User

Id

Email

PasswordHash

DisplayName

Bio

JobRole

Interests (list of strings)

AvatarUrl

Role (User/Admin)

Post

Id

Type (enum: Experience, Idea, ModelGuide, Course, News, Campaign)

Title

Body

Tags (list of strings)

ExternalLink (optional)

AuthorId

CreatedAt

UpdatedAt

IsDeleted

IsFeatured

Comment

Id

PostId

AuthorId

Body

CreatedAt

UpdatedAt (optional)

IsDeleted

Like

Id

PostId

UserId

CreatedAt

CampaignParticipation

Id

PostId (campaign post)

UserId

JoinedAt

10. MVP vs Post-MVP Summary
10.1 MVP Must-Haves (This Spec)

Auth & profiles.

Unified content model with types.

Feed with filters, search, tags.

Comments & likes.

Campaign posts + join.

Admin basic moderation & featured posts.

Production deployment.

10.2 Post-MVP Ideas (For Later)

Advanced admin panel.

Notifications system.

Rich media uploads (images, attachments).

Recommendation engine (personalized feed).

AI-assisted features (e.g. auto-generate tags, summaries).

Multi-language support.
