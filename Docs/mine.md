# Sonic – 4-Week MVP Developer Roadmap

**Goal:** Build and deploy the Sonic MVP – an AI-focused experience sharing platform – in 4 weeks as a polished, portfolio-ready full-stack project.

---

## Week 1 – Backend Foundation & Core Content

**Goal:** Solution structure, Mongo integration, Auth, and basic Post CRUD.

### W1-01 – Create repo & solution layout

* [ ] Create remote repository named `sonic` on your hosting platform.
* [ ] Clone the repository to your local machine.
* [ ] Create backend solution structure:

  * [ ] Create solution file (e.g., `Sonic.sln`).
  * [ ] Create projects:

    * [ ] `Sonic.Api` (API layer).
    * [ ] `Sonic.Application` (application layer, use cases, DTOs, validation).
    * [ ] `Sonic.Domain` (domain entities, enums, business rules).
    * [ ] `Sonic.Infrastructure` (MongoDB, repositories, external services).
  * [ ] Add all projects to the solution.
  * [ ] Configure project references:

    * [ ] `Sonic.Api` → references `Sonic.Application` + `Sonic.Infrastructure`.
    * [ ] `Sonic.Application` → references `Sonic.Domain`.
    * [ ] `Sonic.Infrastructure` → references `Sonic.Domain`.
* [ ] Add `.gitignore` for .NET, Node/Angular, IDE artifacts.
* [ ] Add `.editorconfig` with basic formatting rules.
* [ ] Create minimal `README.md` with:

  * [ ] Project name.
  * [ ] One-paragraph description.
  * [ ] Tech stack summary (.NET, Angular, MongoDB).

### W1-02 – Configure ASP.NET Core API skeleton

* [ ] Decide: controllers vs minimal APIs and note the decision.
* [ ] Enable nullable reference types across backend projects.
* [ ] Configure API startup to:

  * [ ] Register controllers/endpoints.
  * [ ] Configure JSON serialization (property naming, enum handling).
  * [ ] Configure base logging.
* [ ] Set up global error handling mechanism:

  * [ ] Middleware or centralized handler.
  * [ ] Log unhandled exceptions.
  * [ ] Return standardized error responses (no stack traces to client).
* [ ] Implement health check endpoint:

  * [ ] `GET /health` returns a simple OK payload.
* [ ] Verify:

  * [ ] API starts without errors.
  * [ ] `/health` responds as expected.

### W1-03 – MongoDB integration skeleton

* [ ] Decide development MongoDB setup (local Docker / local install / Atlas).
* [ ] Create MongoDB configuration object (connection string, DB name).
* [ ] Add MongoDB config section to API config.
* [ ] Implement MongoDB context/helper:

  * [ ] Create client and get database.
  * [ ] Helper to get typed collections per entity.
* [ ] Register Mongo context and settings in DI:

  * [ ] Bind settings from configuration.
  * [ ] Register Mongo client/context with appropriate lifetimes.
* [ ] Test connectivity:

  * [ ] Run API with Mongo running.
  * [ ] Ensure no connection errors.
  * [ ] Optionally test a dummy collection read/write.

### W1-04 – Domain model for User

* [ ] Define `User` entity in Domain with:

  * [ ] `Id` (document id; decide type and document it).
  * [ ] `Email`.
  * [ ] `PasswordHash`.
  * [ ] `DisplayName`.
  * [ ] `Bio`.
  * [ ] `JobRole`.
  * [ ] `Interests` (list of strings).
  * [ ] `AvatarUrl`.
  * [ ] `Role` (User/Admin).
  * [ ] `CreatedAt`.
  * [ ] `UpdatedAt`.
* [ ] Create `UserRole` enum (User, Admin).
* [ ] Document basic invariants (e.g., Email required, DisplayName required).

### W1-05 – Auth infrastructure (backend)

* [ ] Decide auth approach for MVP: JWT access token (refresh token optional).
* [ ] Add JWT configuration section:

  * [ ] Secret.
  * [ ] Issuer.
  * [ ] Audience.
  * [ ] Access token lifetime.
* [ ] Implement token issuance service:

  * [ ] Builds JWT with user Id, email, role, expiry.
  * [ ] Uses configured issuer, audience, secret.
* [ ] Implement password hashing service:

  * [ ] Secure hashing for new passwords.
  * [ ] Verification method for login.
* [ ] Configure authentication in API:

  * [ ] Add JWT bearer authentication.
  * [ ] Configure token validation parameters.
* [ ] Configure authorization:

  * [ ] Default policy for authenticated endpoints.
  * [ ] Policy or attribute usage for Admin role.
* [ ] Verify startup and a dummy protected endpoint behavior.

### W1-06 – Auth endpoints (register & login)

* [ ] Design auth DTOs:

  * [ ] `RegisterRequest`, `RegisterResponse`.
  * [ ] `LoginRequest`, `LoginResponse`.
* [ ] Define `IUserRepository`:

  * [ ] `GetByEmailAsync`.
  * [ ] `GetByIdAsync`.
  * [ ] `InsertAsync`.
  * [ ] `UpdateAsync`.
* [ ] Implement UserRepository (Infrastructure, Mongo-based).
* [ ] Implement `AuthService` (Application):

  * [ ] `RegisterAsync`:

    * [ ] Validate email format.
    * [ ] Check uniqueness.
    * [ ] Hash password.
    * [ ] Create new User with default `User` role and timestamps.
    * [ ] Save to DB.
  * [ ] `LoginAsync`:

    * [ ] Retrieve by email.
    * [ ] Validate password.
    * [ ] On success, generate JWT.
* [ ] Implement `AuthController`:

  * [ ] `POST /auth/register` calls register.
  * [ ] `POST /auth/login` calls login.
* [ ] Manual test:

  * [ ] Register new user.
  * [ ] Duplicate registration fails.
  * [ ] Login success with correct credentials.
  * [ ] Login fails with wrong password.

### W1-07 – Post domain model

* [ ] Define `PostType` enum:

  * [ ] Experience.
  * [ ] Idea.
  * [ ] ModelGuide.
  * [ ] Course.
  * [ ] News.
  * [ ] Campaign.
* [ ] Define `Post` entity:

  * [ ] `Id`.
  * [ ] `Type` (PostType).
  * [ ] `Title`.
  * [ ] `Body`.
  * [ ] `Tags` (list of strings).
  * [ ] `ExternalLink` (optional).
  * [ ] `AuthorId`.
  * [ ] `CreatedAt`.
  * [ ] `UpdatedAt`.
  * [ ] `IsDeleted`.
  * [ ] `IsFeatured`.
* [ ] Document basic rules:

  * [ ] Title required.
  * [ ] Type required.
  * [ ] ExternalLink usage for Courses/News (if enforced).

### W1-08 – Post repository & application services

* [ ] Design Post DTOs (Application):

  * [ ] `CreatePostRequest`.
  * [ ] `UpdatePostRequest`.
  * [ ] `PostResponse`.
* [ ] Define `IPostRepository`:

  * [ ] `GetByIdAsync`.
  * [ ] `InsertAsync`.
  * [ ] `UpdateAsync`.
  * [ ] `SoftDeleteAsync`.
  * [ ] `QueryAsync` (with filters & pagination).
* [ ] Implement `PostRepository` (Infrastructure, Mongo):

  * [ ] Map Post to collection.
  * [ ] Exclude `IsDeleted` in normal queries.
* [ ] Implement Post use-cases (Application):

  * [ ] `CreatePostAsync`.
  * [ ] `UpdatePostAsync` (with author/admin check).
  * [ ] `DeletePostAsync` (soft delete, author/admin check).
  * [ ] `GetPostByIdAsync`.

### W1-09 – Post API endpoints (CRUD)

* [ ] Implement `PostsController` endpoints:

  * [ ] `POST /posts` (auth required) → create.
  * [ ] `GET /posts/{id}` (public) → details.
  * [ ] `PUT /posts/{id}` (auth, author/admin) → update.
  * [ ] `DELETE /posts/{id}` (auth, author/admin) → soft delete.
* [ ] Ensure consistent mapping between entities and DTOs.
* [ ] Ensure consistent error responses (validation, unauthorized, not found).

### W1-10 – Feed query & pagination

* [ ] Extend `IPostRepository.QueryAsync` to support:

  * [ ] `page`, `pageSize`.
  * [ ] `type` (optional filter).
  * [ ] `tag` (optional filter).
* [ ] Implement ordering by `CreatedAt` descending.
* [ ] Return both items and total count.
* [ ] Implement `GET /posts`:

  * [ ] Accept `page`, `pageSize`, `type`, `tag`.
  * [ ] Return paged list + metadata (page, pageSize, total).

### W1-11 – Simple search

* [ ] Decide MVP search scope (title-only or title+body).
* [ ] Extend repository query to accept optional `q` term.
* [ ] Implement filter for `q` (case-insensitive match).
* [ ] Update `GET /posts` to accept `q` and pass through.
* [ ] Manual tests:

  * [ ] Create posts with different titles.
  * [ ] Verify `q` filters results as expected.

---

## Week 2 – Backend Completion & Hardening

**Goal:** Comments, likes, campaigns, admin features, profile API, docs, and core tests.

### W2-01 – Comment domain model & repository

* [ ] Define `Comment` entity:

  * [ ] `Id`.
  * [ ] `PostId`.
  * [ ] `AuthorId`.
  * [ ] `Body`.
  * [ ] `CreatedAt`.
  * [ ] `UpdatedAt` (optional).
  * [ ] `IsDeleted`.
* [ ] Document rules:

  * [ ] Body required.
  * [ ] PostId required.
* [ ] Define `ICommentRepository`:

  * [ ] Add comment.
  * [ ] Get comments by PostId with pagination.
  * [ ] Soft delete comment.
  * [ ] Get comment by id.
* [ ] Implement CommentRepository (Infrastructure, Mongo):

  * [ ] Map to collection.
  * [ ] Exclude deleted comments in normal queries.
  * [ ] Implement pagination.

### W2-02 – Comment application logic & API

* [ ] Design DTOs:

  * [ ] `CreateCommentRequest`.
  * [ ] `CommentResponse`.
* [ ] Implement comment use-cases (Application):

  * [ ] **AddComment**:

    * [ ] Validate body.
    * [ ] Ensure post exists and not deleted.
    * [ ] Use current user as AuthorId.
  * [ ] **GetCommentsForPost**:

    * [ ] Accept postId, page, pageSize.
    * [ ] Return paginated responses.
  * [ ] **DeleteComment**:

    * [ ] Check author or Admin.
    * [ ] Soft delete.
* [ ] Decide how to include author info in comments (AuthorId + AuthorDisplayName, etc.).
* [ ] Implement API endpoints:

  * [ ] `POST /posts/{postId}/comments` (auth).
  * [ ] `GET /posts/{postId}/comments` (public, paginated).
  * [ ] `DELETE /comments/{id}` (auth, author/admin).
* [ ] Manual tests: add, list, delete comments.

### W2-03 – Like domain model & repository

* [ ] Define `Like` entity:

  * [ ] `Id`.
  * [ ] `PostId`.
  * [ ] `UserId`.
  * [ ] `CreatedAt`.
* [ ] Rules:

  * [ ] A given (`PostId`, `UserId`) pair should exist at most once.
* [ ] Define `ILikeRepository`:

  * [ ] Check if like exists for (`PostId`, `UserId`).
  * [ ] Create like.
  * [ ] Remove like.
  * [ ] Count likes for a post.
* [ ] Implement LikeRepository (Infrastructure, Mongo):

  * [ ] Map to collection.
  * [ ] Implement unique-like logic at repo or DB level.
  * [ ] Efficiently count likes by `PostId`.

### W2-04 – Like application logic & API

* [ ] Decide behavior:

  * [ ] Single toggle endpoint (recommended) or separate like/unlike endpoints; document choice.
* [ ] Implement like use-case (Application):

  * [ ] **ToggleLikeForPost**:

    * [ ] Accept `postId` and current user id.
    * [ ] Ensure post exists and not deleted.
    * [ ] If like exists → remove.
    * [ ] If like does not exist → create.
    * [ ] Return current like count and/or user-like state.
* [ ] Implement API endpoint:

  * [ ] `POST /posts/{postId}/like` (auth):

    * [ ] Calls toggle use-case.
    * [ ] Returns updated like count or state.
* [ ] Integrate like count into Post responses:

  * [ ] Decide if count is included in `PostResponse` or separate endpoint.
* [ ] Manual tests:

  * [ ] Like/unlike a post as the same user and verify count changes correctly.
  * [ ] Like the same post as multiple users and verify counts.

### W2-05 – Campaign domain & participation

* [ ] Confirm `PostType` includes `Campaign`.
* [ ] Decide additional fields for campaigns in `Post`:

  * [ ] e.g., `Goal` (string) and/or `Status` (Open/Closed) – optional for MVP.
* [ ] Add chosen fields to `Post` entity and update persistence.
* [ ] Define `CampaignParticipation` entity:

  * [ ] `Id`.
  * [ ] `PostId` (campaign post).
  * [ ] `UserId`.
  * [ ] `JoinedAt`.
* [ ] Define `ICampaignParticipationRepository`:

  * [ ] Check if participation exists for (`PostId`, `UserId`).
  * [ ] Insert participation.
  * [ ] Count participants for a campaign.
  * [ ] Optionally list participants (for later use).
* [ ] Implement repository (Infrastructure, Mongo):

  * [ ] Map to collection.
  * [ ] Enforce no duplicates for (`PostId`, `UserId`) logically and/or via index.

### W2-06 – Campaign application logic & API

* [ ] Implement campaign-related use-cases:

  * [ ] **CreateCampaignPost**:

    * [ ] Typically use `CreatePost` with `Type = Campaign`.
    * [ ] Validate required fields (title, body, etc.).
  * [ ] **JoinCampaign**:

    * [ ] Accepts `postId` and current user id.
    * [ ] Verify post exists and `Type = Campaign`.
    * [ ] If user already joined, do not duplicate.
    * [ ] Otherwise, create `CampaignParticipation`.
    * [ ] Return updated participant count.
  * [ ] **GetCampaignParticipantsCount**:

    * [ ] Returns count for given `PostId`.
* [ ] Implement API endpoints:

  * [ ] `POST /campaigns/{postId}/join` (auth):

    * [ ] Calls `JoinCampaign`.
    * [ ] Returns participant count or success flag.
  * [ ] `GET /campaigns`:

    * [ ] Simply `GET /posts?type=Campaign` or a dedicated wrapper.
* [ ] Integrate participants count into campaign responses:

  * [ ] Add `ParticipantsCount` to campaign `PostResponse` (for detail view).
* [ ] Manual tests:

  * [ ] Create campaign post.
  * [ ] Join as multiple users and verify participant count, no duplicates.

### W2-07 – Admin moderation endpoints (posts & comments)

* [ ] Verify `UserRole.Admin` is supported in auth logic.
* [ ] Implement admin use-cases:

  * [ ] **AdminDeletePost**:

    * [ ] Check current user is Admin.
    * [ ] Soft-delete target post.
  * [ ] **AdminDeleteComment**:

    * [ ] Check current user is Admin.
    * [ ] Soft-delete target comment.
* [ ] Implement Admin API endpoints:

  * [ ] `DELETE /admin/posts/{id}`:

    * [ ] Requires Admin role.
    * [ ] Uses `AdminDeletePost`.
  * [ ] `DELETE /admin/comments/{id}`:

    * [ ] Requires Admin role.
    * [ ] Uses `AdminDeleteComment`.
* [ ] Manual tests:

  * [ ] As Admin, delete someone else’s post/comment and check it disappears from lists.
  * [ ] As non-Admin, verify access is forbidden.

### W2-08 – Featured posts

* [ ] Confirm `Post` entity includes `IsFeatured` field.
* [ ] Implement featured use-case:

  * [ ] **SetPostFeaturedStatus**:

    * [ ] Accept post id + boolean `isFeatured`.
    * [ ] Require Admin.
    * [ ] Update `IsFeatured`.
* [ ] Implement Admin API:

  * [ ] `POST /admin/posts/{id}/feature`:

    * [ ] Sets `IsFeatured = true`.
  * [ ] (Optional) `POST /admin/posts/{id}/unfeature`:

    * [ ] Sets `IsFeatured = false`.
* [ ] Extend post queries:

  * [ ] Support `featured=true` filter in `GET /posts`.
* [ ] Manual tests:

  * [ ] Mark some posts as featured.
  * [ ] Query with `featured=true` and verify only featured posts returned.

### W2-09 – API documentation (Swagger / OpenAPI)

* [ ] Enable Swagger/OpenAPI generation in API project.
* [ ] Configure Swagger UI:

  * [ ] Set API title (e.g., “Sonic API”).
  * [ ] Group endpoints logically (Auth, Posts, Comments, Campaigns, Admin).
* [ ] Add minimal documentation on main endpoints (summary, remarks).
* [ ] Configure JWT support in Swagger:

  * [ ] Add bearer security definition.
  * [ ] Make protected endpoints require auth in Swagger.
* [ ] Manual tests:

  * [ ] Use Swagger UI to log in and call protected endpoints.

### W2-10 – Core backend tests (happy paths)

* [ ] Decide test framework and structure (e.g., separate `tests` project).
* [ ] Create backend test project (e.g., `Sonic.Tests`).
* [ ] Add tests for **AuthService**:

  * [ ] Register succeeds with valid data.
  * [ ] Register fails when email already exists.
  * [ ] Login succeeds with correct email/password.
  * [ ] Login fails with incorrect password.
* [ ] Add tests for **PostService**:

  * [ ] CreatePost with valid data succeeds.
  * [ ] UpdatePost as owner succeeds.
  * [ ] UpdatePost as non-owner, non-admin is rejected.
  * [ ] DeletePost as owner succeeds.
* [ ] Add tests for **Comment logic**:

  * [ ] AddComment with valid data succeeds.
  * [ ] DeleteComment as author succeeds.
  * [ ] DeleteComment as non-author, non-admin is rejected.
* [ ] Add tests for **Like logic**:

  * [ ] First toggle adds like.
  * [ ] Second toggle removes like.
  * [ ] Like count returns correct number.
* [ ] Add tests for **Campaign join**:

  * [ ] First join creates participation.
  * [ ] Second join does not create duplicate.
* [ ] Ensure tests can run locally without full external infra (test DB or isolated Mongo instance).
* [ ] Run tests and ensure they pass.

### W2-11 – User profile & current user API

* [ ] Design profile DTOs:

  * [ ] `GetCurrentUserResponse` (for `/profile`).
  * [ ] `UpdateProfileRequest` (DisplayName, Bio, JobRole, Interests, AvatarUrl).
* [ ] Implement profile use-cases:

  * [ ] **GetCurrentUser**:

    * [ ] Use authenticated user id.
    * [ ] Fetch via `IUserRepository`.
    * [ ] Map to `GetCurrentUserResponse`.
  * [ ] **UpdateProfile**:

    * [ ] Use authenticated user id.
    * [ ] Validate fields (lengths, optional URL).
    * [ ] Update user fields + `UpdatedAt`.
* [ ] Implement profile API endpoints:

  * [ ] `GET /users/me` (auth required) → current user’s profile.
  * [ ] `PUT /users/me` (auth required) → update profile.
* [ ] Optional public profile:

  * [ ] `GET /users/{id}` → limited info (displayName, avatarUrl, bio, maybe counts).
* [ ] Manual tests:

  * [ ] Fetch own profile.
  * [ ] Update profile and re-fetch to verify.

### W2-12 – Admin user bootstrap

* [ ] Decide how to create the first Admin:

  * [ ] Option A: manually insert Admin user in Mongo.
  * [ ] Option B: startup seed that creates Admin if none exists (using env-configured credentials).
* [ ] Document approach:

  * [ ] In `ARCHITECTURE.md` or `SETUP.md`, note:

    * [ ] How to create/log in as first Admin.
    * [ ] How to change Admin credentials.
* [ ] Manual tests:

  * [ ] Log in as Admin.
  * [ ] Use Admin-only endpoints successfully.

### Optional W2-13 – Backend cleanup & consistency pass

* [ ] Review naming across services, repositories, DTOs.
* [ ] Ensure error messages follow consistent structure.
* [ ] Ensure public endpoints use consistent status codes.
* [ ] Add missing guard clauses/null checks.
* [ ] Update documentation/comments for non-obvious logic.

---

## Week 3 – Angular Frontend & Main UX

**Goal:** Build the Sonic SPA: auth, feed, post detail, create/edit, campaigns, profile.

### W3-01 – Frontend project setup & structure

* [ ] Decide frontend folder location (e.g., `src/sonic-web`).
* [ ] Generate Angular app via CLI.
* [ ] Confirm dev server runs and starter page loads.
* [ ] Choose styling approach:

  * [ ] Plain CSS/SCSS, or
  * [ ] Angular Material, or
  * [ ] Tailwind (document choice).
* [ ] Define folder structure:

  * [ ] `core/` (services, interceptors, layout).
  * [ ] `shared/` (shared components, pipes).
  * [ ] `features/auth/`.
  * [ ] `features/feed/`.
  * [ ] `features/posts/`.
  * [ ] `features/campaigns/`.
  * [ ] `features/profile/`.
* [ ] Define basic routes:

  * [ ] `/login`, `/register`.
  * [ ] `/` or `/feed`.
  * [ ] `/posts/:id`.
  * [ ] `/posts/new`, `/posts/:id/edit`.
  * [ ] `/campaigns`.
  * [ ] `/profile`.

### W3-02 – App shell, layout & navigation

* [ ] Create main layout/shell:

  * [ ] Header (navbar).
  * [ ] Main content area.
  * [ ] Optional footer.
* [ ] Navbar content:

  * [ ] App name/logo (“Sonic”).
  * [ ] Navigation links: Feed, Experiences, Models, Courses, News, Campaigns.
  * [ ] Auth section:

    * [ ] “Login”/“Register” when logged out.
    * [ ] User menu (Profile, Logout) when logged in.
* [ ] Make layout responsive:

  * [ ] Reasonable behavior on small screens.
  * [ ] Proper padding and max-width for main content.

### W3-03 – API configuration & HTTP layer

* [ ] Store API base URL in environment config.
* [ ] Create a central API config or service for base URL.
* [ ] Create a generic HTTP service layer:

  * [ ] Handles base URL prepending.
  * [ ] Provides reusable GET/POST/PUT/DELETE helpers.
* [ ] Plan frontend models to match backend DTOs.

### W3-04 – Client-side auth service & storage

* [ ] Design auth state model:

  * [ ] Token, user id, displayName, email, role.
* [ ] Decide token storage:

  * [ ] localStorage or sessionStorage (document choice).
* [ ] Implement AuthService behavior (conceptually):

  * [ ] `login(credentials)` → call `/auth/login`, store token + user info.
  * [ ] `register(data)` → call `/auth/register`.
  * [ ] `logout()` → clear storage.
  * [ ] `isAuthenticated()` → check token presence/validity.
  * [ ] `getCurrentUser()` / `getRole()` as needed.
* [ ] Design HTTP interceptor:

  * [ ] Attach Bearer token if present.
  * [ ] Handle 401/403 (e.g., redirect to login, clear invalid token).
* [ ] Decide how to restore auth state on app reload.

### W3-05 – Auth screens (login & register)

* [ ] Login page (`/login`):

  * [ ] Form: email, password.
  * [ ] Client validation: required, email format.
  * ```
    [ ] On submit: call AuthService, handle success/failure.
    ```
  * [ ] On success: redirect to feed.
* [ ] Register page (`/register`):

  * [ ] Form: email, password, confirm password, displayName.
  * [ ] Validations: required, format, password length, confirm match.
  * [ ] On submit: call AuthService register.
  * [ ] On success: either auto-login or redirect to login with success message.
* [ ] UX:

  * [ ] Show loading state while request in progress.
  * [ ] Disable button while submitting.
  * [ ] Display backend validation errors.

### W3-06 – Route guards & access control

* [ ] Implement auth guard:

  * [ ] Blocks access if user not authenticated.
  * [ ] Redirects to login (optionally store intended URL).
* [ ] Apply guard to:

  * [ ] Create post.
  * [ ] Edit post.
  * [ ] Profile.
  * [ ] Join campaign.
* [ ] Optional admin guard:

  * [ ] Checks role from auth state.
  * [ ] Protects any future admin-only pages.

### W3-07 – Frontend models (Post, User, Comment, Campaign)

* [ ] Define Post model:

  * [ ] id, type, title, body, tags, externalLink, authorId, authorName, createdAt, updatedAt, isFeatured, likeCount, participantsCount (for campaigns), etc.
* [ ] Define User model:

  * [ ] id, email, displayName, avatarUrl, bio, jobRole, interests, role.
* [ ] Define Comment model:

  * [ ] id, postId, authorId, authorName, body, createdAt, updatedAt.
* [ ] Ensure models match backend responses or mapping is clearly defined.

### W3-08 – Feed page (list of posts with filters)

* [ ] Create Feed page (`/` or `/feed`):

  * [ ] Fetch paginated posts from `GET /posts`.
* [ ] Post list UI:

  * [ ] Card for each post: title, type badge, excerpt, tags, like count, author, date.
  * [ ] Click to view details.
* [ ] Filters:

  * [ ] Filter by type (tabs/buttons).
  * [ ] Tag filter (simple input/dropdown).
* [ ] Search bar:

  * [ ] Search by text (maps to `q`).
* [ ] Pagination:

  * [ ] Buttons for next/previous.
  * [ ] Show current page and total if available.
* [ ] States:

  * [ ] Loading indicator while fetching.
  * [ ] “No posts found” empty state.

### W3-09 – Post detail page (view, likes, comments)

* [ ] Post detail route: `/posts/:id`.
* [ ] Display:

  * [ ] Title, type badge, author, timestamps.
  * [ ] Full body content.
  * [ ] Tags.
  * [ ] External link if present (clickable link).
  * [ ] Like button + like count.
* [ ] Likes:

  * [ ] If authenticated: allow toggling like.
  * [ ] If not: disabled or prompt to log in.
* [ ] Comments:

  * [ ] Show comments list with author and timestamp.
  * [ ] Show comment form if authenticated.
  * [ ] Show prompt to log in if not.
* [ ] Edit/delete options:

  * [ ] Show edit/delete controls for author (and optionally admin).
* [ ] States:

  * [ ] Loading for initial fetch.
  * [ ] “Post not found” state if backend returns 404.

### W3-10 – Create/Edit post form

* [ ] Shared Post form component:

  * [ ] Fields: Title, Type, Body, Tags, ExternalLink.
  * [ ] Validations: required fields, URL format for link.
* [ ] Create post page (`/posts/new`):

  * [ ] Auth required.
  * [ ] On submit: call create endpoint, redirect to detail/feed.
* [ ] Edit post page (`/posts/:id/edit`):

  * [ ] Auth required.
  * [ ] Prefill form from existing post data.
  * [ ] Only allow if current user is author or admin.
  * [ ] On submit: update and redirect.
* [ ] Feedback:

  * [ ] Validation messages.
  * [ ] Disabled submit while sending.
  * [ ] Success/error messages.

### W3-11 – Campaign views & join interaction

* [ ] Campaign list page (`/campaigns`):

  * [ ] Fetch posts with `type=Campaign`.
  * [ ] Show cards with title, goal/summary, participants count.
* [ ] Campaign detail:

  * [ ] Reuse Post detail layout but highlight as Campaign.
  * [ ] Show participants count prominently.
  * [ ] “Join campaign” button if user is authenticated and not already joined.
  * [ ] Disabled or alternate text if already joined or not logged in.
* [ ] On join:

  * [ ] Call join endpoint.
  * [ ] On success: update participants count and button state.
  * [ ] Handle errors gracefully.

### W3-12 – Profile view & edit

* [ ] Profile page (`/profile`), auth required:

  * [ ] Fetch current user via `/users/me`.
  * [ ] Show displayName, email, bio, jobRole, interests, avatar.
* [ ] Edit profile form:

  * [ ] Allow editing DisplayName, Bio, JobRole, Interests, AvatarUrl.
  * [ ] Validate inputs (basic checks).
* [ ] On submit:

  * [ ] Call `PUT /users/me`.
  * [ ] Show success message and refresh data.
* [ ] Optional:

  * [ ] Show list of user’s posts.
  * [ ] Show list of joined campaigns.

### W3-13 – Basic UI polish, loading & error handling

* [ ] Global loading:

  * [ ] Add spinner/skeleton for page-level loads.
* [ ] Global notification/toast pattern:

  * [ ] Success messages for key actions.
  * [ ] Error messages for failures.
* [ ] Empty states for:

  * [ ] No posts in feed.
  * [ ] No comments.
  * [ ] No campaigns.
* [ ] 404 handling:

  * [ ] Generic “Page not found” for unknown routes.
* [ ] Quick consistency check:

  * [ ] Button styles.
  * [ ] Typography hierarchy.
  * [ ] Spacing and alignment.

### W3-14 – End-of-week integration sanity check

* [ ] End-to-end manual tests (frontend + backend):

  * [ ] Register, login.
  * [ ] Create each post type.
  * [ ] Filter and search in feed.
  * [ ] View post details; like, comment.
  * [ ] Create campaign and join.
  * [ ] Edit and delete own posts/comments.
  * [ ] Update profile.
* [ ] Log any bugs or UX issues to fix in Week 4.

---

## Week 4 – Polish, Testing, Deployment & Docs

**Goal:** Make Sonic feel like a real product: polished UI, deployed, documented, with basic tests.

### W4-01 – Visual design & layout polish

* [ ] Full visual pass on all pages.
* [ ] Define simple design system:

  * [ ] Primary/secondary colors.
  * [ ] Typography scale (headings vs body).
  * [ ] Border radius for cards/buttons.
* [ ] Apply design system:

  * [ ] Consistent button styles.
  * [ ] Consistent card styles.
  * [ ] Consistent headings.
* [ ] Fix spacing:

  * [ ] Proper padding in cards/layouts.
  * [ ] Avoid cramped or edge-touching content.
* [ ] Check mobile responsiveness:

  * [ ] Navbar, feed, detail, forms look OK on small screens.

### W4-02 – UX refinement & micro-interactions

* [ ] Review main flows:

  * [ ] Login → feed → detail → back.
  * [ ] Register → create post → view.
  * [ ] Campaign list → join.
  * [ ] Profile edit.
* [ ] Loading indicators:

  * [ ] Feed loading.
  * [ ] Post detail loading.
  * [ ] Form submissions.
* [ ] Success feedback:

  * [ ] On create/edit post.
  * [ ] On comment add/delete.
  * [ ] On join campaign.
  * [ ] On profile update.
* [ ] Error feedback:

  * [ ] Login/registration failures.
  * [ ] Network/timeout issues.
  * [ ] Unauthorized/forbidden.
* [ ] Confirm deletions:

  * [ ] Confirm before deleting posts.
  * [ ] Confirm before deleting comments.
* [ ] Basic keyboard/accessibility checks:

  * [ ] Forms usable with keyboard.
  * [ ] Focus visible on important elements.

### W4-03 – Frontend cleanup & consistency

* [ ] Remove unused components/services/files.
* [ ] Remove debug logs and unused code.
* [ ] Deduplicate UI where possible (shared components).
* [ ] Normalize naming:

  * [ ] Components, services, routes.
* [ ] Ensure type safety:

  * [ ] No unnecessary `any`.
  * [ ] Models align with backend contracts.

### W4-04 – Minimal frontend tests (optional but good)

* [ ] Decide minimal test scope.
* [ ] Add tests:

  * [ ] AuthService: stores/clears token and user.
  * [ ] PostService: builds query params correctly.
  * [ ] One simple component test (e.g., Feed shows list).
* [ ] Ensure tests run via a single command.

### W4-05 – Backend hardening & small refactors

* [ ] Review logging:

  * [ ] Log key events (reg, login fail, post creation, campaign join).
  * [ ] No sensitive data in logs.
* [ ] Review error responses:

  * [ ] All go through global handler.
  * [ ] No stack traces to clients.
  * [ ] Consistent error shape.
* [ ] Security pass:

  * [ ] All writes require auth.
  * [ ] Admin endpoints require Admin.
  * [ ] Author/ownership checked when needed.
  * [ ] Input validation on DTOs.
* [ ] Remove obvious inefficiencies:

  * [ ] Use pagination everywhere.
  * [ ] Avoid dumb N+1 patterns where easy.

### W4-06 – Config & environments (prod vs dev)

* [ ] Identify all environment-specific settings:

  * [ ] API URL (frontend).
  * [ ] Mongo connection string.
  * [ ] JWT secret/issuer/audience.
  * [ ] Any external service URLs.
* [ ] Set up separate configs:

  * [ ] Development.
  * [ ] Production (via env vars or config).
* [ ] Ensure:

  * [ ] Backend reads from env or config.
  * [ ] Frontend uses environment files for base URL.
  * [ ] Secrets are not committed to repo.

### W4-07 – Deployment to cloud

* [ ] Choose provider for:

  * [ ] Backend (e.g., App Service / container host).
  * [ ] Frontend (static hosting).
  * [ ] MongoDB (Atlas or similar).
* [ ] Set up MongoDB cluster:

  * [ ] DB + user + network rules.
* [ ] Deploy backend:

  * [ ] Build in Release.
  * [ ] Configure env vars (Mongo, JWT, etc.).
  * [ ] Verify `/health` on deployed URL.
* [ ] Deploy frontend:

  * [ ] Build production bundle.
  * [ ] Configure base API URL.
  * [ ] Upload to hosting platform.
* [ ] Sanity check:

  * [ ] Hit the real URL.
  * [ ] Register/login, create posts, etc. in production environment.

### W4-08 – Monitoring & logging in production

* [ ] Confirm backend logs visible in hosting platform.
* [ ] Trigger actions and see logs:

  * [ ] Login.
  * [ ] Create post.
  * [ ] Join campaign.
* [ ] Test error responses:

  * [ ] 404, 401/403, validation error.
  * [ ] Ensure they are clean and not leaking internals.
* [ ] If supported, set up health monitoring pointing to `/health`.

### W4-09 – Final QA pass (end-to-end)

* [ ] Define a short test script with all main flows.
* [ ] Execute script in production:

  * [ ] As anonymous visitor.
  * [ ] As normal user.
  * [ ] As admin.
* [ ] Log issues:

  * [ ] Critical (data loss, broken flows) → fix now.
  * [ ] Medium/low (minor UX/visual) → fix if time or push to backlog.

### W4-10 – Documentation (README, architecture, deployment)

* [ ] Update README:

  * [ ] Project description and goals.
  * [ ] Tech stack.
  * [ ] Local setup instructions (backend + frontend).
  * [ ] Test instructions.
  * [ ] Link to live demo.
* [ ] Create `ARCHITECTURE.md`:

  * [ ] High-level architecture diagram/description.
  * [ ] Layer responsibilities (`Api`, `Application`, `Domain`, `Infrastructure`).
  * [ ] Data model summary (User, Post, Comment, Like, CampaignParticipation).
  * [ ] Design decisions (unified Post model, Mongo, JWT).
  * [ ] Future improvements.
* [ ] Create `DEPLOYMENT.md` or section:

  * [ ] How backend and frontend are deployed.
  * [ ] How config/secrets are set.
  * [ ] How to redeploy.

### W4-11 – Interview story & demo preparation

* [ ] Write a short **project pitch**:

  * [ ] What Sonic is, who it’s for, why it exists.
* [ ] List **technical highlights** you’ll talk about:

  * [ ] Architecture.
  * [ ] Data modeling.
  * [ ] Auth/security.
  * [ ] Deployment story.
* [ ] List **trade-offs** you made due to 1-month constraint.
* [ ] Prepare a **demo flow**:

  * [ ] Feed → filter/search.
  * [ ] Post detail → comments/likes.
  * [ ] Campaign → join.
  * [ ] Profile → edit.
  * [ ] (Optional) show admin features.
* [ ] Capture a few **screenshots** for CV/portfolio if you want.