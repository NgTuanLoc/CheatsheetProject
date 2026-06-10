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

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
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
