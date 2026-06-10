# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A personal cheatsheet manager (markdown + uploaded HTML sheets) — .NET 10 Minimal API + PostgreSQL, orchestrated by Aspire. The Next.js frontend (`src/web`) and deployment are planned but **not yet created**; only the backend exists. Authoritative design doc: `docs/superpowers/specs/2026-06-10-cheatsheet-app-design.md`; implementation plans live in `docs/superpowers/plans/`.

## Commands

```powershell
dotnet build                                          # build (solution file is CheatsheetApp.slnx)
dotnet test                                           # all tests — requires Docker (Testcontainers)
dotnet test --filter "FullyQualifiedName~SearchTests" # one test class
dotnet test --filter "DisplayName~prefix match"       # one test by name
dotnet run --project src/CheatsheetApp.AppHost        # run app via Aspire (dashboard + Postgres + API)

# EF Core migrations (DesignTimeDbContextFactory provides the design-time connection)
dotnet ef migrations add <Name> --project src/CheatsheetApp.Api
```

Notes:
- Integration tests spin up a real `postgres:17` container via Testcontainers; Docker must be running. Ryuk is disabled via `.testcontainers.properties` (Docker networking workaround on this machine).
- All integration tests share one `ApiFixture` (one container + one database) via the `[Collection("api")]` fixture — tests must tolerate/avoid data left by other tests; use unique names/slugs per test.

## Architecture

**Vertical slice architecture** — no Services/Repositories layers. Each feature folder under `src/CheatsheetApp.Api/Features/` (Auth, Categories, Cheatsheets, Tags) contains one file per endpoint holding the endpoint class, its handler, and its FluentValidation validator; shared request/response DTOs live in a `Models.cs` per slice. Handlers talk to EF Core (`AppDbContext`) directly.

**Endpoint registration:** each endpoint implements `IEndpoint` (in `Common/`) and is auto-discovered by reflection in `EndpointExtensions.MapEndpoints()`. Endpoints are mapped into a group with `RequireAuthorization()` — **every endpoint requires a JWT by default**; opt out with `.AllowAnonymous()` (only login does). Validation runs via `.AddEndpointFilter<ValidationFilter<TRequest>>()`.

**Response envelope:** every response uses `ApiResponse<T>` `{ success, data, error }` (`Common/ApiResponse.cs`). `GlobalExceptionHandler` converts unhandled exceptions into this envelope with sanitized messages; details go to Serilog.

**Cross-cutting in `Common/`:** envelope, exception handler, `IEndpoint` + registration, `ValidationFilter`, `SlugGenerator`.

**Data layer (`Data/`):** entities + `AppDbContext` + EF migrations. Full-text search is a stored computed `tsvector` column on cheatsheets (title weighted 'A', content 'B' so title matches outrank body) with a GIN index. `DbInitializer` runs at startup: applies migrations, then seeds the single admin user from `Admin:Username`/`Admin:Password` config if no users exist.

**Configuration / secrets:** `Jwt:Key` is required and validated at startup (fail fast). In dev, the AppHost injects dev-only values for `Jwt__Key`, `Admin__Username`, `Admin__Password` (`AppHost.cs`); tests set them in `ApiFixture`. Login is rate-limited (fixed window, 5/minute, policy name `"login"`).

## Conventions

- Slugs are auto-generated (`SlugGenerator`): category slugs globally unique; cheatsheet slugs unique **per category**. Hence the detail route is `GET /cheatsheets/{categorySlug}/{slug}` (a deliberate deviation from the spec's `/cheatsheets/{slug}`).
- Deleting a category with cheatsheets is rejected (`DeleteBehavior.Restrict`); cheatsheet deletion is a hard delete.
- Import (`POST /cheatsheets/import`) accepts `.md`/`.html` up to 2 MB; export returns a file matching the sheet's `content_type` (`markdown` | `html`).
- New tests: integration tests go in `tests/.../Integration` using `[Collection("api")]` + `ApiFixture.CreateAuthenticatedClientAsync()`; pure logic (e.g. slug rules) gets unit tests in `Unit/`.
- OpenAPI + Scalar UI are mapped in Development only.
