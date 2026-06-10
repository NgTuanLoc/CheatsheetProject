# Cheatsheet App — Design

**Date:** 2026-06-10
**Status:** Approved

## Overview

A personal cheatsheet manager: create, organize, search, and read cheatsheets written in markdown or uploaded as styled HTML files. Single user, deployed on a VPS behind a login. Stack: .NET 10 API + Next.js 16 frontend + PostgreSQL, orchestrated by Aspire.

## Goals

- Store cheatsheets organized by category (one each) and tags (many).
- Create and edit markdown cheatsheets in-app with a modern editor; import existing `.md` files.
- Upload `.html` cheatsheets and render them with their own CSS intact.
- Full-text search across all content.
- Syntax-highlighted code blocks with one-click copy.
- Dark/light mode; glassmorphism UI (Aurora theme) with a token-based theme system so more themes (e.g., Slate) can be added without component changes.
- Deployable to a VPS with Docker Compose.

## Non-Goals (v1)

- Multi-user accounts, sharing, or public pages.
- Nested category hierarchies.
- In-app editing of HTML cheatsheets (re-upload to update).
- Soft delete / trash.
- Versioning / history of cheatsheets.

## Architecture

.NET 10 Minimal API owns PostgreSQL via EF Core 10; Next.js 16 is the entire UI and calls the API server-side only (BFF pattern — the browser never talks to the API directly). Aspire orchestrates everything in dev and publishes docker-compose artifacts for deployment.

### Solution structure

```
CheatsheetProject/
├── CheatsheetApp.sln
├── src/
│   ├── CheatsheetApp.AppHost/         # Aspire 13.x orchestrator (dev entry point)
│   ├── CheatsheetApp.Api/             # .NET 10 Minimal API + EF Core 10 + Npgsql
│   ├── CheatsheetApp.ServiceDefaults/ # Aspire telemetry/health/resilience defaults
│   └── web/                           # Next.js 16 App Router + Tailwind v4 + shadcn/ui
├── tests/
│   └── CheatsheetApp.Api.Tests/       # xUnit + Testcontainers
└── docs/superpowers/specs/
```

Frontend tests (Vitest, Playwright) live inside `src/web`.

### Key technology choices

| Concern | Choice |
|---|---|
| Backend | .NET 10 LTS, Minimal APIs, EF Core 10 + Npgsql |
| Orchestration | Aspire 13.x (dev dashboard; docker-compose publisher for deploy) |
| Frontend | Next.js 16 (App Router, React 19), Tailwind CSS v4, shadcn/ui |
| Markdown editor | Milkdown (Crepe) |
| Markdown rendering | unified/remark in React Server Components |
| Syntax highlighting | Shiki (dual light/dark themes) |
| Theming | next-themes + CSS custom property tokens |
| Search | PostgreSQL full-text search (tsvector + GIN) |
| Logging | Serilog (structured) |
| Validation | FluentValidation |
| Auth | BCrypt password hash, JWT, HttpOnly cookie held by Next.js |

## Data Model (PostgreSQL, EF Core migrations)

- **users** — `id, username, password_hash (BCrypt), created_at`. Single row in practice; no registration flow. Initial admin credentials seeded from environment configuration at first startup.
- **categories** — `id, name, slug, icon, sort_order`. Flat list.
- **cheatsheets** — `id, category_id (FK), title, slug, content_type ('markdown' | 'html'), content (text), created_at, updated_at`. HTML uploads store the full file (including `<style>` blocks and inline CSS) in `content`.
- **tags** — `id, name, slug`.
- **cheatsheet_tags** — many-to-many join table.

Search: a generated `tsvector` column over `title + content` with a GIN index. Title matches rank above body matches; prefix matching supported ("dock" finds Docker).

Rules:
- Slugs are auto-generated from names/titles. Category slugs are globally unique; cheatsheet slugs are unique within their category. URLs look like `/sheets/git/undo-last-commit`.
- Deleting a category that still contains cheatsheets is rejected (reassign first).
- Deleting a cheatsheet is a hard delete behind a confirm dialog.

## API

All endpoints require a valid JWT except `POST /auth/login`. Responses use a consistent envelope `{ success, data, error }`.

| Area | Endpoints |
|---|---|
| Auth | `POST /auth/login` — verifies credentials, returns JWT (7-day expiry); rate-limited to 5 attempts/minute via .NET rate limiter |
| Categories | `GET /categories`, `POST /categories`, `PUT /categories/{id}`, `DELETE /categories/{id}` |
| Cheatsheets | `GET /cheatsheets?category=&tag=&q=`, `GET /cheatsheets/{slug}`, `POST /cheatsheets`, `PUT /cheatsheets/{id}`, `DELETE /cheatsheets/{id}` |
| Tags | `GET /tags` (with usage counts) |
| Files | `POST /cheatsheets/import` (.md/.html upload, 2 MB max), `GET /cheatsheets/{id}/export` (downloads .md or .html matching the sheet's content type) |

Input validation with FluentValidation at the boundary. Uploads are checked for extension, size, and well-formedness before storage. OpenAPI document + Scalar UI in dev only.

## Auth Flow (BFF)

1. Browser submits credentials to a Next.js route handler.
2. Next.js forwards to `POST /auth/login`; API verifies BCrypt hash and returns a JWT.
3. Next.js stores the JWT in an HttpOnly, Secure, SameSite=strict cookie. Client JS never sees it.
4. Next.js middleware redirects unauthenticated visitors to `/login`; all server-side API fetches attach the JWT as a Bearer token.

## Frontend

### Layout (docs-style sidebar)

- Persistent left sidebar: collapsible category groups listing their sheets, active sheet highlighted, "+ New", import button, theme/dark-mode toggle.
- `/login` — credentials form.
- `/` — redirects to the most recently updated cheatsheet.
- `/sheets/[category]/[slug]` — reading view: rendered content, tag chips, Edit / Export / Delete actions.
- `/sheets/new`, `/sheets/[category]/[slug]/edit` — Milkdown editor with title field, category picker, tag input. Markdown sheets only; for HTML sheets the edit page instead shows title/category/tags fields plus a "Replace file" upload that sends the new content through `PUT /cheatsheets/{id}`.
- Search: command palette (Ctrl+K, shadcn/ui Command) querying the API's full-text search; Enter navigates to the sheet. No separate search page.

### Rendering

- **Markdown:** server-rendered in RSCs via unified/remark + Shiki. Every code block gets a copy button. Zero client JS for reading.
- **HTML sheets:** rendered in a sandboxed `<iframe>` (`sandbox` attribute, scripts disabled, content via `srcDoc`). The iframe is both the style-isolation boundary (sheet CSS can't leak in or out) and the XSS safety boundary.

### Visual design — Aurora glass

Glassmorphism: frosted translucent panels (backdrop blur, low-opacity white borders) over a dark backdrop with vivid indigo/pink/cyan gradient orbs; gradient accents on active sidebar items and tag chips; glass code blocks. Light mode mirrors the same treatment on a light backdrop.

### Theme system

- All colors, blur strengths, border opacities, and gradients are CSS custom-property tokens; components reference only tokens.
- Tailwind v4 CSS-first `@theme` maps tokens to utilities.
- A theme is one CSS block under `[data-theme="<name>"]` with light and dark variants. `next-themes` manages the attribute and dark/light switching (defaults to system preference).
- v1 ships Aurora only; adding a theme (e.g., Slate) = one new token block, zero component changes.

## Error Handling

- API: global exception handler → envelope with correct HTTP status; full detail logged via Serilog, sanitized messages to clients.
- Frontend: error boundaries per route segment; toast notifications (Sonner) for failed mutations; inline validation errors on forms; explicit "couldn't reach API" state.

## Testing (80% coverage target)

- **API:** xUnit integration tests against real PostgreSQL via Testcontainers — auth, CRUD, search ranking, import/export, rate limiting. Unit tests for slug generation and validators.
- **Frontend:** Vitest + React Testing Library for sidebar, editor wrapper, command palette. 
- **E2E (Playwright):** login → create sheet → search (Ctrl+K) → copy code block; import .md; upload .html and verify sandboxed render with its own styles.

## Deployment

- `aspire publish` (docker-compose publisher) → compose file with: API container, Next.js standalone container, Postgres with named volume.
- VPS: Caddy reverse proxy for automatic HTTPS; Postgres not exposed publicly.
- Secrets (JWT signing key, DB password, initial admin credentials) via `.env`; never committed.
- EF Core migrations apply on API startup.
- Backups: nightly `pg_dump` cron job; manual .md export as escape hatch.
