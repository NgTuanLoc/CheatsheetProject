# Cheatsheet App — Plan 1 of 3: Backend API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the complete .NET 10 backend — Aspire orchestration, PostgreSQL data layer, and a vertical-slice Minimal API (auth, categories, cheatsheets, tags, search, import/export) — fully covered by integration tests.

**Architecture:** Vertical slice architecture: each feature folder under `Features/` owns its endpoints, request/response models, validators, and handlers, talking to EF Core directly. Cross-cutting concerns (auth, response envelope, validation, error handling) live in `Common/`. Aspire AppHost orchestrates PostgreSQL + API in dev. Integration tests run against real PostgreSQL via Testcontainers.

**Tech Stack:** .NET 10 LTS, Aspire 13.x, Minimal APIs, EF Core 10 + Npgsql, PostgreSQL full-text search, BCrypt.Net-Next, JWT Bearer, FluentValidation, Serilog, xUnit + Testcontainers, Scalar (dev API docs).

**Spec:** `docs/superpowers/specs/2026-06-10-cheatsheet-app-design.md`. Plans 2 (frontend) and 3 (deployment) will be written after this plan is executed.

**Conventions for all tasks:**
- Run all commands from repo root `D:\Projects\CheatsheetProject` (PowerShell).
- Every API response uses the `ApiResponse<T>` envelope `{ success, data, error }`.
- One deviation from the spec table: cheatsheet detail route is `GET /cheatsheets/{categorySlug}/{slug}` (not `/cheatsheets/{slug}`) because sheet slugs are only unique per category.

---

### Task 1: Solution scaffolding

**Files:**
- Create: `CheatsheetApp.sln`, `src/CheatsheetApp.AppHost/`, `src/CheatsheetApp.ServiceDefaults/`, `src/CheatsheetApp.Api/`, `tests/CheatsheetApp.Api.Tests/`

- [ ] **Step 1: Verify .NET 10 SDK and create projects**

```powershell
dotnet --version   # expect 10.x
dotnet new sln -n CheatsheetApp
dotnet new aspire-apphost -o src/CheatsheetApp.AppHost
dotnet new aspire-servicedefaults -o src/CheatsheetApp.ServiceDefaults
dotnet new web -o src/CheatsheetApp.Api
dotnet new xunit -o tests/CheatsheetApp.Api.Tests
dotnet sln add src/CheatsheetApp.AppHost src/CheatsheetApp.ServiceDefaults src/CheatsheetApp.Api tests/CheatsheetApp.Api.Tests
```

If `aspire-apphost` template is missing, install templates first: `dotnet new install Aspire.ProjectTemplates`.

- [ ] **Step 2: Wire project references**

```powershell
dotnet add src/CheatsheetApp.Api reference src/CheatsheetApp.ServiceDefaults
dotnet add src/CheatsheetApp.AppHost reference src/CheatsheetApp.Api
dotnet add tests/CheatsheetApp.Api.Tests reference src/CheatsheetApp.Api
```

- [ ] **Step 3: Add NuGet packages**

```powershell
dotnet add src/CheatsheetApp.AppHost package Aspire.Hosting.PostgreSQL
dotnet add src/CheatsheetApp.Api package Aspire.Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/CheatsheetApp.Api package Microsoft.EntityFrameworkCore.Design
dotnet add src/CheatsheetApp.Api package BCrypt.Net-Next
dotnet add src/CheatsheetApp.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/CheatsheetApp.Api package FluentValidation.DependencyInjectionExtensions
dotnet add src/CheatsheetApp.Api package Serilog.AspNetCore
dotnet add src/CheatsheetApp.Api package Microsoft.AspNetCore.OpenApi
dotnet add src/CheatsheetApp.Api package Scalar.AspNetCore
dotnet add tests/CheatsheetApp.Api.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/CheatsheetApp.Api.Tests package Testcontainers.PostgreSql
```

- [ ] **Step 4: Verify it builds**

Run: `dotnet build`
Expected: `Build succeeded` (warnings OK at this stage).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "chore: scaffold solution with Aspire, API, and test projects"
```

---

### Task 2: AppHost orchestration (PostgreSQL + API)

**Files:**
- Modify: `src/CheatsheetApp.AppHost/AppHost.cs` (template may name it `Program.cs` — replace whichever exists)

- [ ] **Step 1: Replace AppHost entry point**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb();

var db = postgres.AddDatabase("cheatsheets");

builder.AddProject<Projects.CheatsheetApp_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    // Dev-only values. Production values come from .env (Plan 3).
    .WithEnvironment("Admin__Username", "admin")
    .WithEnvironment("Admin__Password", "dev-password-change-me")
    .WithEnvironment("Jwt__Key", "dev-only-jwt-signing-key-0123456789abcdef0123456789abcdef");

builder.Build().Run();
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

(Full `dotnet run --project src/CheatsheetApp.AppHost` smoke test happens in Task 13, once the API actually uses the database.)

- [ ] **Step 3: Commit**

```powershell
git add -A
git commit -m "feat: orchestrate postgres and api in Aspire AppHost"
```

---

### Task 3: Slug generator (TDD)

**Files:**
- Create: `src/CheatsheetApp.Api/Common/SlugGenerator.cs`
- Test: `tests/CheatsheetApp.Api.Tests/Unit/SlugGeneratorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Unit;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Undo Last Commit", "undo-last-commit")]
    [InlineData("  C#  LINQ   Tricks!  ", "c-linq-tricks")]
    [InlineData("Café déjà-vu", "cafe-deja-vu")]
    [InlineData("docker_compose.v2", "docker-compose-v2")]
    [InlineData("!!!", "untitled")]
    public void Generate_produces_url_safe_slug(string input, string expected)
    {
        var slug = SlugGenerator.Generate(input);

        Assert.Equal(expected, slug);
    }
}
```

Delete the template's `UnitTest1.cs`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter SlugGeneratorTests`
Expected: FAIL — compile error, `SlugGenerator` does not exist.

- [ ] **Step 3: Implement**

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CheatsheetApp.Api.Common;

public static partial class SlugGenerator
{
    public static string Generate(string input)
    {
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsAsciiLetterOrDigit(ch))
                sb.Append(ch);
            else if (ch is ' ' or '-' or '_' or '.')
                sb.Append('-');
        }

        var slug = CollapseDashes().Replace(sb.ToString(), "-").Trim('-');
        return slug.Length == 0 ? "untitled" : slug;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseDashes();
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter SlugGeneratorTests`
Expected: PASS, 5 test cases.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: add slug generator with diacritic and symbol handling"
```

---

### Task 4: Data layer — entities, DbContext, migration, seeding

**Files:**
- Create: `src/CheatsheetApp.Api/Data/User.cs`, `Data/Category.cs`, `Data/Cheatsheet.cs`, `Data/Tag.cs`, `Data/AppDbContext.cs`, `Data/DesignTimeDbContextFactory.cs`, `Data/DbInitializer.cs`

- [ ] **Step 1: Create entities**

`src/CheatsheetApp.Api/Data/User.cs`:

```csharp
namespace CheatsheetApp.Api.Data;

public sealed class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

`src/CheatsheetApp.Api/Data/Category.cs`:

```csharp
namespace CheatsheetApp.Api.Data;

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public List<Cheatsheet> Cheatsheets { get; set; } = [];
}
```

`src/CheatsheetApp.Api/Data/Cheatsheet.cs`:

```csharp
using NpgsqlTypes;

namespace CheatsheetApp.Api.Data;

public static class ContentTypes
{
    public const string Markdown = "markdown";
    public const string Html = "html";
    public static readonly string[] All = [Markdown, Html];
}

public sealed class Cheatsheet
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string ContentType { get; set; }
    public required string Content { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<Tag> Tags { get; set; } = [];
}
```

`src/CheatsheetApp.Api/Data/Tag.cs`:

```csharp
namespace CheatsheetApp.Api.Data;

public sealed class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public List<Cheatsheet> Cheatsheets { get; set; } = [];
}
```

- [ ] **Step 2: Create AppDbContext**

`src/CheatsheetApp.Api/Data/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cheatsheet> Cheatsheets => Set<Cheatsheet>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Username).IsUnique();
            b.Property(u => u.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Name).HasMaxLength(100);
            b.Property(c => c.Slug).HasMaxLength(120);
        });

        modelBuilder.Entity<Cheatsheet>(b =>
        {
            b.HasIndex(c => new { c.CategoryId, c.Slug }).IsUnique();
            b.Property(c => c.Title).HasMaxLength(200);
            b.Property(c => c.Slug).HasMaxLength(240);
            b.Property(c => c.ContentType).HasMaxLength(10);
            b.HasOne(c => c.Category).WithMany(cat => cat.Cheatsheets)
                .HasForeignKey(c => c.CategoryId).OnDelete(DeleteBehavior.Restrict);
            // Title weighted 'A', content 'B' so title matches outrank body matches in ts_rank.
            b.Property(c => c.SearchVector)
                .HasColumnType("tsvector")
                .HasComputedColumnSql(
                    "setweight(to_tsvector('english', coalesce(title, '')), 'A') || " +
                    "setweight(to_tsvector('english', coalesce(content, '')), 'B')",
                    stored: true);
            b.HasIndex(c => c.SearchVector).HasMethod("GIN");
        });

        modelBuilder.Entity<Tag>(b =>
        {
            b.HasIndex(t => t.Slug).IsUnique();
            b.Property(t => t.Name).HasMaxLength(60);
            b.Property(t => t.Slug).HasMaxLength(80);
            b.HasMany(t => t.Cheatsheets).WithMany(c => c.Tags);
        });
    }
}
```

- [ ] **Step 3: Design-time factory (for `dotnet ef` commands only)**

`src/CheatsheetApp.Api/Data/DesignTimeDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CheatsheetApp.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=cheatsheets;Username=postgres;Password=design-time-only")
            .Options;
        return new AppDbContext(options);
    }
}
```

- [ ] **Step 4: Migration + seeding on startup**

`src/CheatsheetApp.Api/Data/DbInitializer.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            var username = config["Admin:Username"]
                ?? throw new InvalidOperationException("Admin:Username is not configured.");
            var password = config["Admin:Password"]
                ?? throw new InvalidOperationException("Admin:Password is not configured.");

            db.Users.Add(new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 5: Generate the migration**

Install EF tool if needed: `dotnet tool install --global dotnet-ef`

```powershell
dotnet ef migrations add InitialCreate --project src/CheatsheetApp.Api
```

Expected: `Data/Migrations/` (or `Migrations/`) folder appears with `*_InitialCreate.cs`. Open it and confirm the cheatsheets table has the computed `search_vector`-style tsvector column with `setweight(...)` SQL and a GIN index.

- [ ] **Step 6: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "feat: add data model, DbContext with weighted tsvector search, migration, seeding"
```

---

### Task 5: Common infrastructure + Program.cs assembly

**Files:**
- Create: `src/CheatsheetApp.Api/Common/ApiResponse.cs`, `Common/IEndpoint.cs`, `Common/EndpointExtensions.cs`, `Common/GlobalExceptionHandler.cs`, `Common/ValidationFilter.cs`, `Features/Auth/JwtTokenFactory.cs`
- Modify: `src/CheatsheetApp.Api/Program.cs` (replace template content entirely)

- [ ] **Step 1: Response envelope**

`src/CheatsheetApp.Api/Common/ApiResponse.cs`:

```csharp
namespace CheatsheetApp.Api.Common;

public sealed record ApiResponse<T>(bool Success, T? Data, string? Error)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
}
```

- [ ] **Step 2: Endpoint discovery**

`src/CheatsheetApp.Api/Common/IEndpoint.cs`:

```csharp
namespace CheatsheetApp.Api.Common;

public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
```

`src/CheatsheetApp.Api/Common/EndpointExtensions.cs`:

```csharp
namespace CheatsheetApp.Api.Common;

public static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        // All endpoints require auth by default; opt out with .AllowAnonymous().
        var group = app.MapGroup("").RequireAuthorization();

        var endpoints = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IEndpoint)) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IEndpoint)Activator.CreateInstance(t)!);

        foreach (var endpoint in endpoints)
            endpoint.Map(group);
    }
}
```

- [ ] **Step 3: Global exception handler**

`src/CheatsheetApp.Api/Common/GlobalExceptionHandler.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics;

namespace CheatsheetApp.Api.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail("An unexpected error occurred."), cancellationToken);
        return true;
    }
}
```

- [ ] **Step 4: Validation filter**

`src/CheatsheetApp.Api/Common/ValidationFilter.cs`:

```csharp
using FluentValidation;

namespace CheatsheetApp.Api.Common;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is not null)
        {
            var model = context.Arguments.OfType<T>().FirstOrDefault();
            if (model is null)
                return Results.BadRequest(ApiResponse<object>.Fail("Invalid request body."));

            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
                return Results.BadRequest(ApiResponse<object>.Fail(
                    string.Join("; ", result.Errors.Select(e => e.ErrorMessage))));
        }
        return await next(context);
    }
}
```

- [ ] **Step 5: JWT token factory**

`src/CheatsheetApp.Api/Features/Auth/JwtTokenFactory.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CheatsheetApp.Api.Features.Auth;

public static class JwtTokenFactory
{
    public const string Issuer = "cheatsheet-api";
    public const string Audience = "cheatsheet-app";

    public static string Create(string username, string signingKey)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, username)],
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 6: Assemble Program.cs (replace entire file)**

`src/CheatsheetApp.Api/Program.cs`:

```csharp
using System.Text;
using System.Threading.RateLimiting;
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using CheatsheetApp.Api.Features.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("cheatsheets");

// Fail fast if secrets are missing (validated at startup, not first use).
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = JwtTokenFactory.Issuer,
        ValidAudience = JwtTokenFactory.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
    });
builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", window =>
    {
        window.PermitLimit = 5;
        window.Window = TimeSpan.FromMinutes(1);
        window.QueueLimit = 0;
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler(_ => { });
app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await DbInitializer.InitializeAsync(app.Services);

app.Run();

public partial class Program;
```

- [ ] **Step 7: Verify build**

Run: `dotnet build`
Expected: `Build succeeded`.

- [ ] **Step 8: Commit**

```powershell
git add -A
git commit -m "feat: add response envelope, endpoint discovery, validation, error handling, JWT, Program assembly"
```

---

### Task 6: Integration test infrastructure

**Files:**
- Create: `tests/CheatsheetApp.Api.Tests/Integration/ApiFixture.cs`, `Integration/ApiCollection.cs`, `Integration/HealthTests.cs`

- [ ] **Step 1: Write the fixture**

`tests/CheatsheetApp.Api.Tests/Integration/ApiFixture.cs`:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CheatsheetApp.Api.Tests.Integration;

public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "integration-test-password";
    public const string JwtKey = "integration-test-jwt-key-0123456789abcdef0123456789abcdef";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    private string? _token;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:cheatsheets", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Admin:Username", AdminUsername);
        builder.UseSetting("Admin:Password", AdminPassword);
    }

    /// <summary>Client with a valid Bearer token (logs in once, then caches).</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();
        if (_token is null)
        {
            var response = await client.PostAsJsonAsync("/auth/login",
                new { username = AdminUsername, password = AdminPassword });
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>();
            _token = body!.Data!.Token;
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return client;
    }

    private sealed record LoginPayload(string Token);

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
```

Note: `CreateAuthenticatedClientAsync` won't work until Task 7 adds the login endpoint — that's expected; this task's test only uses `/health`.

Note: if the test project was generated with **xunit.v3** (check the `.csproj` for `xunit.v3` package), `IAsyncLifetime` methods return `ValueTask` instead of `Task` — change both signatures accordingly (`public async ValueTask InitializeAsync()` / `async ValueTask IAsyncLifetime.DisposeAsync()`).

`tests/CheatsheetApp.Api.Tests/Integration/ApiCollection.cs`:

```csharp
namespace CheatsheetApp.Api.Tests.Integration;

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
```

- [ ] **Step 2: Write the health test**

`tests/CheatsheetApp.Api.Tests/Integration/HealthTests.cs`:

```csharp
namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class HealthTests(ApiFixture fixture)
{
    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 3: Run the test (Docker must be running)**

Run: `dotnet test --filter HealthTests`
Expected: PASS. This proves: container starts, app boots, migration applies, admin user seeds.

If it fails with a Docker connection error, start Docker Desktop first.

- [ ] **Step 4: Commit**

```powershell
git add -A
git commit -m "test: add Testcontainers-backed integration test infrastructure"
```

---

### Task 7: Auth slice — login endpoint

**Files:**
- Create: `src/CheatsheetApp.Api/Features/Auth/Login.cs`
- Test: `tests/CheatsheetApp.Api.Tests/Integration/AuthTests.cs`, `Integration/RateLimitTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/CheatsheetApp.Api.Tests/Integration/AuthTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class AuthTests(ApiFixture fixture)
{
    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new { username = ApiFixture.AdminUsername, password = ApiFixture.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginPayload>>();
        Assert.True(body!.Success);
        Assert.False(string.IsNullOrWhiteSpace(body.Data!.Token));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login",
            new { username = ApiFixture.AdminUsername, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.False(body!.Success);
    }

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record LoginPayload(string Token);
}
```

`tests/CheatsheetApp.Api.Tests/Integration/RateLimitTests.cs` — note: **own fixture instance**, not the shared collection, because it deliberately exhausts the login rate limit:

```csharp
using System.Net;
using System.Net.Http.Json;

namespace CheatsheetApp.Api.Tests.Integration;

public class RateLimitTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Sixth_login_attempt_within_a_minute_returns_429()
    {
        var client = fixture.CreateClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/login",
                new { username = "admin", password = "wrong" });
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "AuthTests|RateLimitTests"`
Expected: FAIL — `/auth/login` returns 404 (endpoint doesn't exist). The `Protected_endpoint_without_token_returns_401` test also fails with 404 for now (categories endpoint arrives in Task 8 — re-run this test there if it still fails here; if your routing returns 401 from the auth middleware before 404, it may already pass, which is fine).

- [ ] **Step 3: Implement the login slice**

`src/CheatsheetApp.Api/Features/Auth/Login.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record LoginResponse(string Token);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Password).NotEmpty().MaximumLength(200);
    }
}

public sealed class LoginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/auth/login", HandleAsync)
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

    private static async Task<IResult> HandleAsync(
        LoginRequest request, AppDbContext db, IConfiguration config, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Results.Json(
                ApiResponse<object>.Fail("Invalid username or password."),
                statusCode: StatusCodes.Status401Unauthorized);

        var token = JwtTokenFactory.Create(user.Username, config["Jwt:Key"]!);
        return Results.Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse(token)));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "AuthTests|RateLimitTests"`
Expected: PASS (the protected-endpoint test passes because JWT middleware rejects before routing 404s — if it still returns 404, defer that one assertion to Task 8 and re-enable it there).

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: add login slice with BCrypt verification, JWT issuing, rate limiting"
```

---

### Task 8: Categories slices

**Files:**
- Create: `src/CheatsheetApp.Api/Features/Categories/Models.cs`, `Features/Categories/GetCategories.cs`, `Features/Categories/CreateCategory.cs`, `Features/Categories/UpdateCategory.cs`, `Features/Categories/DeleteCategory.cs`
- Test: `tests/CheatsheetApp.Api.Tests/Integration/CategoryTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/CheatsheetApp.Api.Tests/Integration/CategoryTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class CategoryTests(ApiFixture fixture)
{
    public sealed record CategoryPayload(int Id, string Name, string Slug, string? Icon, int SortOrder);

    [Fact]
    public async Task Create_then_list_returns_category_with_generated_slug()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/categories",
            new { name = "Docker Compose", icon = "🐳", sortOrder = 1 });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>();
        Assert.Equal("docker-compose", created!.Data!.Slug);

        var list = await client.GetFromJsonAsync<ApiResponse<List<CategoryPayload>>>("/categories");
        Assert.Contains(list!.Data!, c => c.Slug == "docker-compose");
    }

    [Fact]
    public async Task Create_with_duplicate_name_returns_409()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/categories", new { name = "DupCat", sortOrder = 0 });

        var second = await client.PostAsJsonAsync("/categories", new { name = "DupCat", sortOrder = 0 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Update_renames_category_and_regenerates_slug()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var create = await client.PostAsJsonAsync("/categories", new { name = "Renameable", sortOrder = 0 });
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>())!.Data!.Id;

        var update = await client.PutAsJsonAsync($"/categories/{id}",
            new { name = "Renamed Category", icon = "📦", sortOrder = 5 });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>();
        Assert.Equal("renamed-category", updated!.Data!.Slug);
    }

    [Fact]
    public async Task Delete_empty_category_succeeds_and_missing_returns_404()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var create = await client.PostAsJsonAsync("/categories", new { name = "Deletable", sortOrder = 0 });
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>())!.Data!.Id;

        var delete = await client.DeleteAsync($"/categories/{id}");
        var deleteAgain = await client.DeleteAsync($"/categories/{id}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
    }
}
```

(The "delete category with sheets returns 409" test lands in Task 9, once cheatsheets can be created.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter CategoryTests`
Expected: FAIL — 404s, endpoints don't exist.

- [ ] **Step 3: Implement the slices**

`src/CheatsheetApp.Api/Features/Categories/Models.cs`:

```csharp
using CheatsheetApp.Api.Data;

namespace CheatsheetApp.Api.Features.Categories;

public sealed record CategoryResponse(int Id, string Name, string Slug, string? Icon, int SortOrder)
{
    public static CategoryResponse From(Category c) => new(c.Id, c.Name, c.Slug, c.Icon, c.SortOrder);
}

public sealed record SaveCategoryRequest(string Name, string? Icon, int SortOrder);
```

`src/CheatsheetApp.Api/Features/Categories/GetCategories.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class GetCategoriesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app.MapGet("/categories", HandleAsync);

    private static async Task<IResult> HandleAsync(AppDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => CategoryResponse.From(c))
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<List<CategoryResponse>>.Ok(categories));
    }
}
```

`src/CheatsheetApp.Api/Features/Categories/CreateCategory.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class SaveCategoryRequestValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Icon).MaximumLength(20);
    }
}

public sealed class CreateCategoryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/categories", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCategoryRequest>>();

    private static async Task<IResult> HandleAsync(
        SaveCategoryRequest request, AppDbContext db, CancellationToken ct)
    {
        var slug = SlugGenerator.Generate(request.Name);
        if (await db.Categories.AnyAsync(c => c.Slug == slug, ct))
            return Results.Conflict(ApiResponse<object>.Fail("A category with this name already exists."));

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/categories/{category.Id}",
            ApiResponse<CategoryResponse>.Ok(CategoryResponse.From(category)));
    }
}
```

`src/CheatsheetApp.Api/Features/Categories/UpdateCategory.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class UpdateCategoryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/categories/{id:int}", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCategoryRequest>>();

    private static async Task<IResult> HandleAsync(
        int id, SaveCategoryRequest request, AppDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        var slug = SlugGenerator.Generate(request.Name);
        if (await db.Categories.AnyAsync(c => c.Slug == slug && c.Id != id, ct))
            return Results.Conflict(ApiResponse<object>.Fail("A category with this name already exists."));

        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.Icon = request.Icon;
        category.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<CategoryResponse>.Ok(CategoryResponse.From(category)));
    }
}
```

`src/CheatsheetApp.Api/Features/Categories/DeleteCategory.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class DeleteCategoryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/categories/{id:int}", HandleAsync);

    private static async Task<IResult> HandleAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        if (await db.Cheatsheets.AnyAsync(c => c.CategoryId == id, ct))
            return Results.Conflict(ApiResponse<object>.Fail(
                "Category still contains cheatsheets. Reassign them first."));

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object?>.Ok(null));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter CategoryTests`
Expected: PASS, 4 tests. Also re-run `dotnet test --filter AuthTests` — the protected-endpoint 401 test must now pass.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: add category CRUD slices with slug generation and delete protection"
```

---

### Task 9: Cheatsheets CRUD slices

**Files:**
- Create: `src/CheatsheetApp.Api/Features/Cheatsheets/Models.cs`, `Features/Cheatsheets/TagResolver.cs`, `Features/Cheatsheets/SheetSlug.cs`, `Features/Cheatsheets/CreateCheatsheet.cs`, `Features/Cheatsheets/GetCheatsheet.cs`, `Features/Cheatsheets/UpdateCheatsheet.cs`, `Features/Cheatsheets/DeleteCheatsheet.cs`
- Test: `tests/CheatsheetApp.Api.Tests/Integration/CheatsheetCrudTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/CheatsheetApp.Api.Tests/Integration/CheatsheetCrudTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class CheatsheetCrudTests(ApiFixture fixture)
{
    public sealed record SheetPayload(
        int Id, string Title, string Slug, string CategorySlug, string ContentType,
        string Content, string[] Tags, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
    private sealed record CategoryPayload(int Id, string Slug);

    private async Task<(HttpClient Client, int CategoryId, string CategorySlug)> SetupAsync(string categoryName)
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/categories", new { name = categoryName, sortOrder = 0 });
        var category = (await response.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>())!.Data!;
        return (client, category.Id, category.Slug);
    }

    [Fact]
    public async Task Create_returns_sheet_with_slug_and_tags()
    {
        var (client, categoryId, categorySlug) = await SetupAsync("CrudCat A");

        var create = await client.PostAsJsonAsync("/cheatsheets", new
        {
            title = "Undo Last Commit",
            categoryId,
            contentType = "markdown",
            content = "## Soft reset\n\n```bash\ngit reset --soft HEAD~1\n```",
            tags = new[] { "git", "Recovery" },
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var sheet = (await create.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!;
        Assert.Equal("undo-last-commit", sheet.Slug);
        Assert.Equal(categorySlug, sheet.CategorySlug);
        Assert.Equal(2, sheet.Tags.Length);
    }

    [Fact]
    public async Task Duplicate_title_in_same_category_gets_suffixed_slug()
    {
        var (client, categoryId, _) = await SetupAsync("CrudCat B");
        var body = new { title = "Same Title", categoryId, contentType = "markdown", content = "x", tags = Array.Empty<string>() };

        await client.PostAsJsonAsync("/cheatsheets", body);
        var second = await client.PostAsJsonAsync("/cheatsheets", body);

        var sheet = (await second.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!;
        Assert.Equal("same-title-2", sheet.Slug);
    }

    [Fact]
    public async Task Get_by_category_and_slug_returns_sheet()
    {
        var (client, categoryId, categorySlug) = await SetupAsync("CrudCat C");
        await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Findable", categoryId, contentType = "markdown", content = "find me", tags = Array.Empty<string>() });

        var response = await client.GetAsync($"/cheatsheets/{categorySlug}/findable");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sheet = (await response.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!;
        Assert.Equal("find me", sheet.Content);
    }

    [Fact]
    public async Task Update_changes_content_tags_and_slug()
    {
        var (client, categoryId, _) = await SetupAsync("CrudCat D");
        var create = await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Old Title", categoryId, contentType = "markdown", content = "old", tags = new[] { "old-tag" } });
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!.Id;

        var update = await client.PutAsJsonAsync($"/cheatsheets/{id}", new
        { title = "New Title", categoryId, contentType = "markdown", content = "new", tags = new[] { "new-tag" } });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var sheet = (await update.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!;
        Assert.Equal("new-title", sheet.Slug);
        Assert.Equal(["new-tag"], sheet.Tags);
        Assert.Equal("new", sheet.Content);
    }

    [Fact]
    public async Task Delete_removes_sheet_and_invalid_content_type_rejected()
    {
        var (client, categoryId, categorySlug) = await SetupAsync("CrudCat E");
        var create = await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Doomed", categoryId, contentType = "markdown", content = "x", tags = Array.Empty<string>() });
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!.Id;

        var delete = await client.DeleteAsync($"/cheatsheets/{id}");
        var getAfter = await client.GetAsync($"/cheatsheets/{categorySlug}/doomed");
        var badType = await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Bad", categoryId, contentType = "pdf", content = "x", tags = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfter.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badType.StatusCode);
    }

    [Fact]
    public async Task Delete_category_containing_sheets_returns_409()
    {
        var (client, categoryId, _) = await SetupAsync("CrudCat F");
        await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Blocker", categoryId, contentType = "markdown", content = "x", tags = Array.Empty<string>() });

        var delete = await client.DeleteAsync($"/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter CheatsheetCrudTests`
Expected: FAIL — 404s on `/cheatsheets`.

- [ ] **Step 3: Implement shared slice models and helpers**

`src/CheatsheetApp.Api/Features/Cheatsheets/Models.cs`:

```csharp
using CheatsheetApp.Api.Data;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed record CheatsheetDetail(
    int Id, string Title, string Slug, string CategorySlug, string CategoryName,
    string ContentType, string Content, string[] Tags,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static CheatsheetDetail From(Cheatsheet c) => new(
        c.Id, c.Title, c.Slug, c.Category.Slug, c.Category.Name,
        c.ContentType, c.Content,
        c.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToArray(),
        c.CreatedAt, c.UpdatedAt);
}

public sealed record CheatsheetSummary(
    int Id, string Title, string Slug, string CategorySlug, string CategoryName,
    string ContentType, string[] Tags, DateTimeOffset UpdatedAt);

public sealed record SaveCheatsheetRequest(
    string Title, int CategoryId, string ContentType, string Content, string[] Tags);
```

`src/CheatsheetApp.Api/Features/Cheatsheets/TagResolver.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public static class TagResolver
{
    /// <summary>Finds existing tags by slug or creates new ones. Caller saves changes.</summary>
    public static async Task<List<Tag>> ResolveAsync(
        AppDbContext db, IEnumerable<string> names, CancellationToken ct)
    {
        var distinct = names
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .DistinctBy(SlugGenerator.Generate)
            .ToList();

        var slugs = distinct.Select(SlugGenerator.Generate).ToList();
        var existing = await db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync(ct);

        var result = new List<Tag>(existing);
        foreach (var name in distinct)
        {
            var slug = SlugGenerator.Generate(name);
            if (existing.All(t => t.Slug != slug))
            {
                var tag = new Tag { Name = name, Slug = slug };
                db.Tags.Add(tag);
                result.Add(tag);
            }
        }
        return result;
    }
}
```

`src/CheatsheetApp.Api/Features/Cheatsheets/SheetSlug.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public static class SheetSlug
{
    /// <summary>Generates a slug unique within the category, appending -2, -3, ... on collision.</summary>
    public static async Task<string> GenerateUniqueAsync(
        AppDbContext db, int categoryId, string title, int? excludeId, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.Generate(title);
        var slug = baseSlug;
        var suffix = 2;
        while (await db.Cheatsheets.AnyAsync(
            c => c.CategoryId == categoryId && c.Slug == slug && (excludeId == null || c.Id != excludeId), ct))
        {
            slug = $"{baseSlug}-{suffix++}";
        }
        return slug;
    }
}
```

- [ ] **Step 4: Implement the CRUD slices**

`src/CheatsheetApp.Api/Features/Cheatsheets/CreateCheatsheet.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class SaveCheatsheetRequestValidator : AbstractValidator<SaveCheatsheetRequest>
{
    public SaveCheatsheetRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.ContentType)
            .Must(t => ContentTypes.All.Contains(t))
            .WithMessage("ContentType must be 'markdown' or 'html'.");
        RuleFor(r => r.Content).NotEmpty();
        RuleFor(r => r.Tags).NotNull();
        RuleForEach(r => r.Tags).MaximumLength(60);
    }
}

public sealed class CreateCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/cheatsheets", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCheatsheetRequest>>();

    private static async Task<IResult> HandleAsync(
        SaveCheatsheetRequest request, AppDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        var now = DateTimeOffset.UtcNow;
        var sheet = new Cheatsheet
        {
            Title = request.Title.Trim(),
            CategoryId = category.Id,
            Category = category,
            Slug = await SheetSlug.GenerateUniqueAsync(db, category.Id, request.Title, null, ct),
            ContentType = request.ContentType,
            Content = request.Content,
            CreatedAt = now,
            UpdatedAt = now,
            Tags = await TagResolver.ResolveAsync(db, request.Tags, ct),
        };
        db.Cheatsheets.Add(sheet);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/cheatsheets/{category.Slug}/{sheet.Slug}",
            ApiResponse<CheatsheetDetail>.Ok(CheatsheetDetail.From(sheet)));
    }
}
```

`src/CheatsheetApp.Api/Features/Cheatsheets/GetCheatsheet.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class GetCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/cheatsheets/{categorySlug}/{slug}", HandleAsync);

    private static async Task<IResult> HandleAsync(
        string categorySlug, string slug, AppDbContext db, CancellationToken ct)
    {
        var sheet = await db.Cheatsheets.AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Category.Slug == categorySlug && c.Slug == slug, ct);

        return sheet is null
            ? Results.NotFound(ApiResponse<object>.Fail("Cheatsheet not found."))
            : Results.Ok(ApiResponse<CheatsheetDetail>.Ok(CheatsheetDetail.From(sheet)));
    }
}
```

`src/CheatsheetApp.Api/Features/Cheatsheets/UpdateCheatsheet.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class UpdateCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/cheatsheets/{id:int}", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCheatsheetRequest>>();

    private static async Task<IResult> HandleAsync(
        int id, SaveCheatsheetRequest request, AppDbContext db, CancellationToken ct)
    {
        var sheet = await db.Cheatsheets
            .Include(c => c.Category)
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == id, ct);
        if (sheet is null)
            return Results.NotFound(ApiResponse<object>.Fail("Cheatsheet not found."));

        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        sheet.Title = request.Title.Trim();
        sheet.CategoryId = category.Id;
        sheet.Category = category;
        sheet.Slug = await SheetSlug.GenerateUniqueAsync(db, category.Id, request.Title, id, ct);
        sheet.ContentType = request.ContentType;
        sheet.Content = request.Content;
        sheet.UpdatedAt = DateTimeOffset.UtcNow;
        sheet.Tags.Clear();
        sheet.Tags.AddRange(await TagResolver.ResolveAsync(db, request.Tags, ct));
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<CheatsheetDetail>.Ok(CheatsheetDetail.From(sheet)));
    }
}
```

`src/CheatsheetApp.Api/Features/Cheatsheets/DeleteCheatsheet.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class DeleteCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/cheatsheets/{id:int}", HandleAsync);

    private static async Task<IResult> HandleAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var sheet = await db.Cheatsheets.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (sheet is null)
            return Results.NotFound(ApiResponse<object>.Fail("Cheatsheet not found."));

        db.Cheatsheets.Remove(sheet);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object?>.Ok(null));
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter CheatsheetCrudTests`
Expected: PASS, 6 tests.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat: add cheatsheet CRUD slices with per-category slugs and tag resolution"
```

---

### Task 10: List & full-text search slice

**Files:**
- Create: `src/CheatsheetApp.Api/Features/Cheatsheets/ListCheatsheets.cs`
- Test: `tests/CheatsheetApp.Api.Tests/Integration/SearchTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/CheatsheetApp.Api.Tests/Integration/SearchTests.cs`:

```csharp
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class SearchTests(ApiFixture fixture) : IAsyncLifetime
{
    private HttpClient _client = null!;
    private sealed record CategoryPayload(int Id, string Slug);
    public sealed record SummaryPayload(
        int Id, string Title, string Slug, string CategorySlug, string CategoryName,
        string ContentType, string[] Tags, DateTimeOffset UpdatedAt);

    public async Task InitializeAsync()
    {
        _client = await fixture.CreateAuthenticatedClientAsync();
        var response = await _client.PostAsJsonAsync("/categories", new { name = "SearchCat", sortOrder = 0 });
        var categoryId = (await response.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>())!.Data!.Id;

        await _client.PostAsJsonAsync("/cheatsheets", new
        {
            title = "Kubernetes Basics", categoryId, contentType = "markdown",
            content = "pods and deployments", tags = new[] { "searchable" },
        });
        await _client.PostAsJsonAsync("/cheatsheets", new
        {
            title = "Service Mesh", categoryId, contentType = "markdown",
            content = "istio runs on kubernetes clusters", tags = Array.Empty<string>(),
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_ranks_title_match_above_content_match()
    {
        var result = await _client.GetFromJsonAsync<ApiResponse<List<SummaryPayload>>>(
            "/cheatsheets?q=kubernetes");

        var titles = result!.Data!.Select(s => s.Title).ToList();
        Assert.Equal(2, titles.Count(t => t is "Kubernetes Basics" or "Service Mesh"));
        Assert.True(titles.IndexOf("Kubernetes Basics") < titles.IndexOf("Service Mesh"),
            "title match should rank above content match");
    }

    [Fact]
    public async Task Search_supports_prefix_matching()
    {
        var result = await _client.GetFromJsonAsync<ApiResponse<List<SummaryPayload>>>(
            "/cheatsheets?q=kuber");

        Assert.Contains(result!.Data!, s => s.Title == "Kubernetes Basics");
    }

    [Fact]
    public async Task Filter_by_category_and_tag_narrows_results()
    {
        var byCategory = await _client.GetFromJsonAsync<ApiResponse<List<SummaryPayload>>>(
            "/cheatsheets?category=searchcat");
        var byTag = await _client.GetFromJsonAsync<ApiResponse<List<SummaryPayload>>>(
            "/cheatsheets?tag=searchable");

        Assert.Equal(2, byCategory!.Data!.Count);
        Assert.Single(byTag!.Data!);
        Assert.Equal("Kubernetes Basics", byTag.Data![0].Title);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter SearchTests`
Expected: FAIL — `GET /cheatsheets` returns 404.

- [ ] **Step 3: Implement the list/search slice**

`src/CheatsheetApp.Api/Features/Cheatsheets/ListCheatsheets.cs`:

```csharp
using System.Text.RegularExpressions;
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed partial class ListCheatsheetsEndpoint : IEndpoint
{
    private const int MaxSearchTerms = 8;

    public void Map(IEndpointRouteBuilder app) => app.MapGet("/cheatsheets", HandleAsync);

    private static async Task<IResult> HandleAsync(
        string? category, string? tag, string? q, AppDbContext db, CancellationToken ct)
    {
        var query = db.Cheatsheets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category.Slug == category);
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(c => c.Tags.Any(t => t.Slug == tag));

        var tsQuery = string.IsNullOrWhiteSpace(q) ? null : ToPrefixTsQuery(q);
        query = string.IsNullOrEmpty(tsQuery)
            ? query.OrderByDescending(c => c.UpdatedAt)
            : query
                .Where(c => c.SearchVector.Matches(EF.Functions.ToTsQuery("english", tsQuery)))
                .OrderByDescending(c => c.SearchVector.Rank(EF.Functions.ToTsQuery("english", tsQuery)));

        var items = await query
            .Select(c => new CheatsheetSummary(
                c.Id, c.Title, c.Slug, c.Category.Slug, c.Category.Name, c.ContentType,
                c.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToArray(), c.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<List<CheatsheetSummary>>.Ok(items));
    }

    /// <summary>Sanitizes free text into a safe tsquery: lexemes ANDed, each prefix-matched.</summary>
    private static string ToPrefixTsQuery(string input)
    {
        var terms = TermPattern().Matches(input.ToLowerInvariant())
            .Select(m => m.Value)
            .Take(MaxSearchTerms)
            .ToArray();
        return terms.Length == 0 ? "" : string.Join(" & ", terms.Select(t => $"{t}:*"));
    }

    [GeneratedRegex("[a-z0-9]+")]
    private static partial Regex TermPattern();
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter SearchTests`
Expected: PASS, 3 tests.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: add list endpoint with category/tag filters and weighted prefix full-text search"
```

---

### Task 11: Tags slice

**Files:**
- Create: `src/CheatsheetApp.Api/Features/Tags/GetTags.cs`
- Test: `tests/CheatsheetApp.Api.Tests/Integration/TagTests.cs`

- [ ] **Step 1: Write failing test**

`tests/CheatsheetApp.Api.Tests/Integration/TagTests.cs`:

```csharp
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class TagTests(ApiFixture fixture)
{
    private sealed record CategoryPayload(int Id);
    public sealed record TagPayload(int Id, string Name, string Slug, int UsageCount);

    [Fact]
    public async Task Tags_endpoint_returns_tags_with_usage_counts()
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/categories", new { name = "TagCat", sortOrder = 0 });
        var categoryId = (await response.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>())!.Data!.Id;
        await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Tagged One", categoryId, contentType = "markdown", content = "x", tags = new[] { "counted-tag" } });
        await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Tagged Two", categoryId, contentType = "markdown", content = "x", tags = new[] { "counted-tag" } });

        var tags = await client.GetFromJsonAsync<ApiResponse<List<TagPayload>>>("/tags");

        var counted = tags!.Data!.Single(t => t.Slug == "counted-tag");
        Assert.Equal(2, counted.UsageCount);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter TagTests`
Expected: FAIL — `GET /tags` returns 404.

- [ ] **Step 3: Implement**

`src/CheatsheetApp.Api/Features/Tags/GetTags.cs`:

```csharp
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Tags;

public sealed record TagResponse(int Id, string Name, string Slug, int UsageCount);

public sealed class GetTagsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app.MapGet("/tags", HandleAsync);

    private static async Task<IResult> HandleAsync(AppDbContext db, CancellationToken ct)
    {
        var tags = await db.Tags.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagResponse(t.Id, t.Name, t.Slug, t.Cheatsheets.Count))
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<List<TagResponse>>.Ok(tags));
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter TagTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -A
git commit -m "feat: add tags endpoint with usage counts"
```

---

### Task 12: Import & export slices

**Files:**
- Create: `src/CheatsheetApp.Api/Features/Cheatsheets/ImportCheatsheet.cs`, `Features/Cheatsheets/ExportCheatsheet.cs`
- Test: `tests/CheatsheetApp.Api.Tests/Integration/ImportExportTests.cs`

- [ ] **Step 1: Write failing tests**

`tests/CheatsheetApp.Api.Tests/Integration/ImportExportTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class ImportExportTests(ApiFixture fixture)
{
    private sealed record CategoryPayload(int Id);
    public sealed record SheetPayload(int Id, string Title, string Slug, string ContentType, string Content);

    private async Task<(HttpClient Client, int CategoryId)> SetupAsync(string categoryName)
    {
        var client = await fixture.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/categories", new { name = categoryName, sortOrder = 0 });
        return (client, (await response.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>())!.Data!.Id);
    }

    private static MultipartFormDataContent BuildUpload(string fileName, string content, int categoryId)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        form.Add(file, "file", fileName);
        form.Add(new StringContent(categoryId.ToString()), "categoryId");
        return form;
    }

    [Fact]
    public async Task Import_markdown_file_creates_markdown_sheet()
    {
        var (client, categoryId) = await SetupAsync("ImportCat A");

        var response = await client.PostAsync("/cheatsheets/import",
            BuildUpload("My Git Notes.md", "# Git\n\nnotes here", categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sheet = (await response.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!;
        Assert.Equal("My Git Notes", sheet.Title);
        Assert.Equal("markdown", sheet.ContentType);
    }

    [Fact]
    public async Task Import_html_file_creates_html_sheet()
    {
        var (client, categoryId) = await SetupAsync("ImportCat B");

        var response = await client.PostAsync("/cheatsheets/import",
            BuildUpload("styled.html", "<html><style>h1{color:red}</style><h1>Hi</h1></html>", categoryId));

        var sheet = (await response.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!;
        Assert.Equal("html", sheet.ContentType);
        Assert.Contains("<style>", sheet.Content);
    }

    [Fact]
    public async Task Import_rejects_unsupported_extension_and_oversized_file()
    {
        var (client, categoryId) = await SetupAsync("ImportCat C");

        var badExt = await client.PostAsync("/cheatsheets/import",
            BuildUpload("notes.pdf", "binary stuff", categoryId));
        var tooBig = await client.PostAsync("/cheatsheets/import",
            BuildUpload("big.md", new string('x', 3 * 1024 * 1024), categoryId));

        Assert.Equal(HttpStatusCode.BadRequest, badExt.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);
    }

    [Fact]
    public async Task Export_downloads_markdown_with_correct_content_type()
    {
        var (client, categoryId) = await SetupAsync("ImportCat D");
        var create = await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Exportable", categoryId, contentType = "markdown", content = "## export me", tags = Array.Empty<string>() });
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!.Id;

        var response = await client.GetAsync($"/cheatsheets/{id}/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/markdown", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("## export me", await response.Content.ReadAsStringAsync());
        Assert.Contains("exportable.md", response.Content.Headers.ContentDisposition!.FileName);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter ImportExportTests`
Expected: FAIL — 404s.

- [ ] **Step 3: Implement import**

`src/CheatsheetApp.Api/Features/Cheatsheets/ImportCheatsheet.cs`:

```csharp
using System.Text;
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class ImportCheatsheetEndpoint : IEndpoint
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".md"] = ContentTypes.Markdown,
        [".markdown"] = ContentTypes.Markdown,
        [".html"] = ContentTypes.Html,
        [".htm"] = ContentTypes.Html,
    };

    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/cheatsheets/import", HandleAsync).DisableAntiforgery();

    private static async Task<IResult> HandleAsync(
        IFormFile file, [FromForm] int categoryId, AppDbContext db, CancellationToken ct)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!ExtensionMap.TryGetValue(extension, out var contentType))
            return Results.BadRequest(ApiResponse<object>.Fail(
                "Unsupported file type. Upload .md, .markdown, .html, or .htm."));

        if (file.Length is 0 or > MaxFileSizeBytes)
            return Results.BadRequest(ApiResponse<object>.Fail("File must be between 1 byte and 2 MB."));

        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == categoryId, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        string content;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            content = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(content))
            return Results.BadRequest(ApiResponse<object>.Fail("File is empty."));

        var title = Path.GetFileNameWithoutExtension(file.FileName).Trim();
        var now = DateTimeOffset.UtcNow;
        var sheet = new Cheatsheet
        {
            Title = title,
            CategoryId = category.Id,
            Category = category,
            Slug = await SheetSlug.GenerateUniqueAsync(db, category.Id, title, null, ct),
            ContentType = contentType,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Cheatsheets.Add(sheet);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/cheatsheets/{category.Slug}/{sheet.Slug}",
            ApiResponse<CheatsheetDetail>.Ok(CheatsheetDetail.From(sheet)));
    }
}
```

- [ ] **Step 4: Implement export**

`src/CheatsheetApp.Api/Features/Cheatsheets/ExportCheatsheet.cs`:

```csharp
using System.Text;
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class ExportCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/cheatsheets/{id:int}/export", HandleAsync);

    private static async Task<IResult> HandleAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var sheet = await db.Cheatsheets.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, ct);
        if (sheet is null)
            return Results.NotFound(ApiResponse<object>.Fail("Cheatsheet not found."));

        var isMarkdown = sheet.ContentType == ContentTypes.Markdown;
        return Results.File(
            Encoding.UTF8.GetBytes(sheet.Content),
            contentType: isMarkdown ? "text/markdown" : "text/html",
            fileDownloadName: $"{sheet.Slug}{(isMarkdown ? ".md" : ".html")}");
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter ImportExportTests`
Expected: PASS, 4 tests. Note: the 3 MB upload test relies on the request reaching the handler; if Kestrel rejects it first with 413, change the assertion to accept either 400 or 413 — both are correct rejections.

- [ ] **Step 6: Commit**

```powershell
git add -A
git commit -m "feat: add import/export slices with extension, size, and emptiness validation"
```

---

### Task 13: Full verification + Aspire smoke test

**Files:** none new.

- [ ] **Step 1: Run the entire test suite**

Run: `dotnet test`
Expected: ALL tests pass (unit + integration).

- [ ] **Step 2: Aspire smoke test (manual, requires Docker)**

```powershell
dotnet run --project src/CheatsheetApp.AppHost
```

Expected: Aspire dashboard URL prints; open it; `postgres`, `cheatsheets` DB, and `api` resources all go green/healthy. Open the api resource's Scalar endpoint (`/scalar/v1` on the API's URL) and execute `POST /auth/login` with `admin` / `dev-password-change-me` — expect a token back. Ctrl+C to stop.

- [ ] **Step 3: Commit any stragglers and tag completion**

```powershell
git add -A
git commit -m "chore: backend API complete" --allow-empty
```

---

## Done — definition for this plan

- `dotnet test` fully green against real PostgreSQL.
- AppHost boots Postgres + API with healthy dashboard.
- Every spec'd endpoint implemented in vertical slices with the `ApiResponse` envelope.
- Next: Plan 2 (Next.js frontend with Aurora glass theme system), then Plan 3 (docker-compose deployment).
