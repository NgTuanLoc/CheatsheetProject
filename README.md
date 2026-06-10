# Cheatsheet App

A personal cheatsheet manager — store, search, and organize markdown and HTML reference sheets by category and tag.

## Stack

| Layer | Tech |
|---|---|
| API | .NET 10 Minimal API, vertical slice architecture |
| ORM | EF Core 10 + Npgsql |
| Database | PostgreSQL 17 (full-text search via `tsvector`) |
| Auth | JWT Bearer (HS256), BCrypt, rate limiting |
| Orchestration | .NET Aspire 13.4.3 |
| Tests | xUnit v2 + Testcontainers.PostgreSql |
| Frontend | Next.js 16 + Tailwind v4 *(Plan 2 — not yet built)* |

## Project layout

```
src/
  CheatsheetApp.AppHost/        # Aspire orchestration (Postgres + API)
  CheatsheetApp.ServiceDefaults/ # Shared Aspire telemetry/health defaults
  CheatsheetApp.Api/
    Common/                     # ApiResponse envelope, IEndpoint, ValidationFilter, SlugGenerator
    Data/                       # EF Core entities, DbContext, DbInitializer, migration
    Features/
      Auth/                     # POST /auth/login
      Categories/               # GET/POST/PUT/DELETE /categories
      Cheatsheets/              # CRUD, list+search, import, export
      Tags/                     # GET /tags
tests/
  CheatsheetApp.Api.Tests/
    Unit/                       # SlugGeneratorTests
    Integration/                # ApiFixture (Testcontainers), all endpoint tests
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Testcontainers + Aspire Postgres)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling): `dotnet workload install aspire`
- [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) *(migrations only)*: `dotnet tool install --global dotnet-ef`

## Running locally

```bash
dotnet run --project src/CheatsheetApp.AppHost
```

Aspire starts PostgreSQL and the API, then opens the dashboard at **https://localhost:17221**.  
Default dev credentials — **username:** `admin` **password:** `dev-password-change-me`

Interactive API docs (Scalar) are available at the API's `/scalar/v1` URL shown in the dashboard.

## Running tests

```bash
dotnet test tests/CheatsheetApp.Api.Tests
```

Integration tests spin up a real PostgreSQL 17 container via Testcontainers. Docker must be running.

## API reference

All responses use the envelope `{ "success": bool, "data": T | null, "error": string | null }`.

| Method | Route | Description |
|---|---|---|
| `POST` | `/auth/login` | Obtain a JWT token |
| `GET` | `/categories` | List all categories |
| `POST` | `/categories` | Create a category |
| `PUT` | `/categories/{id}` | Update a category |
| `DELETE` | `/categories/{id}` | Delete a category (fails if sheets exist) |
| `GET` | `/cheatsheets` | List/search sheets (`?q=`, `?category=`, `?tag=`) |
| `POST` | `/cheatsheets` | Create a cheatsheet |
| `GET` | `/cheatsheets/{categorySlug}/{slug}` | Get a single cheatsheet |
| `PUT` | `/cheatsheets/{id}` | Update a cheatsheet |
| `DELETE` | `/cheatsheets/{id}` | Delete a cheatsheet |
| `POST` | `/cheatsheets/import` | Upload a `.md` or `.html` file (multipart, max 2 MB) |
| `GET` | `/cheatsheets/{id}/export` | Download the raw file |
| `GET` | `/tags` | List all tags with usage counts |

All endpoints except `/auth/login` require `Authorization: Bearer <token>`.

### Full-text search

`GET /cheatsheets?q=kubernetes` uses PostgreSQL `tsvector` search with prefix matching. Title matches rank above content matches (weight A vs B).

## Configuration

Dev values are injected by the AppHost. For production, supply these environment variables:

| Variable | Description |
|---|---|
| `ConnectionStrings__cheatsheets` | PostgreSQL connection string |
| `Jwt__Key` | HS256 signing key (min 32 chars) |
| `Admin__Username` | Seed admin username |
| `Admin__Password` | Seed admin password |

## Roadmap

- **Plan 2** — Next.js 16 frontend: Aurora glass UI, Milkdown markdown editor, Shiki syntax highlighting, Ctrl+K command palette
- **Plan 3** — Docker Compose deployment: Caddy reverse proxy, `.env` secrets, nightly `pg_dump`
