# Sonic

Sonic is a place where people share **how they actually use AI in real work**.

Not another “AI blog” or “prompt dump”.

It’s about things like:

- “I’m a designer, here’s how I use AI to speed up concepting.”
- “I’m a recruiter, here’s how I screen CVs faster without being unfair.”
- “Here’s the best model + workflow I found for summarizing legal docs.”
- “We want to run a small campaign to test AI in our team – who wants in?”

Sonic is built as a portfolio-friendly, production-ish app: clean architecture, real auth, a proper front-end, and a real database behind it.

---

## What you can do on Sonic

### 1. Share real-world AI experiences

Post how you use AI in your job:

- What you do (role, context).
- Which tools/models you use.
- What worked, what didn’t.
- Tips, prompts, workflows.

These show up as **Experience** posts in the feed.

---

### 2. Share ideas and experiments

You don’t need to have it all figured out.

You can post:

- Half-baked ideas.
- “What if we used AI for X at work?”
- Early prototypes and experiments.

These live as **Idea** posts and can attract feedback and discussion.

---

### 3. Explain which AI model works best for what

Not everyone knows whether to use:

- GPT-style LLMs  
- Vision models  
- Local models  
- Specialized APIs  

Sonic lets you publish **Model Guides** that answer questions like:

> “If you’re doing customer support triage, here’s the model, the setup, and the caveats.”

---

### 4. Collect learning resources

You can attach:

- Courses  
- Tutorials  
- Articles  
- Playlists  

as **Course** posts or **News** posts with a short summary and a link.

The goal: not yet another link dump, but **curated “why this matters for your work”**.

---

### 5. Run and join campaigns

Campaigns are special posts where people:

- Rally around a problem (“Let’s try AI for documentation in my team for 2 weeks”)
- Ask for help on a real challenge
- Try to coordinate small experiments

Users can **join** campaigns so you see a participant count, not just likes.

---

## Who Sonic is for

- People who already use AI at work and want to share what actually works.
- People who are lost in “AI noise” and want **practical**, job-specific examples.
- Teams who want to run small experiments together.
- Recruiters / hiring managers who want to see **real projects** (this app itself is one).

---

## Tech in one glance

- **Backend:** ASP.NET Core, MongoDB
- **Frontend:** Angular, TypeScript  
- **Architecture:** Clean-ish layered setup (Api / Application / Domain / Infrastructure)  
- **Docs:** Swagger/OpenAPI for the API

It’s not a toy script – it’s built to look like something you could drop into a junior/mid dev interview and walk through confidently.

---

## High-level architecture

Backend layers:

- `Sonic.Api` – HTTP endpoints, auth setup, global error handling, Swagger.
- `Sonic.Application` – use cases (Auth, Posts, Comments, Likes, Campaigns, Profile), DTOs, business flows.
- `Sonic.Domain` – entities (`User`, `Post`, `Comment`, `Like`, `CampaignParticipation`), enums (`PostType`, `UserRole`), core rules.
- `Sonic.Infrastructure` – MongoDB setup and repository implementations.

Frontend structure (Angular):

- `core/` – layout, auth guard, HTTP interceptor, config.
- `shared/` – reusable UI parts.
- `features/auth/` – login & register.
- `features/feed/` – main feed, filters, search.
- `features/posts/` – post detail, create/edit.
- `features/campaigns/` – campaign list & join.
- `features/profile/` – profile view & edit.