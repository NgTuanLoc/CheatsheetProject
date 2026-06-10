using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class SearchTests(ApiFixture fixture) : IAsyncLifetime
{
    // Guard against InitializeAsync running multiple times per test-session for the shared fixture.
    private static readonly SemaphoreSlim _seedLock = new(1, 1);
    private static bool _seeded;

    private HttpClient _client = null!;
    private sealed record CategoryPayload(int Id, string Slug);
    public sealed record SummaryPayload(
        int Id, string Title, string Slug, string CategorySlug, string CategoryName,
        string ContentType, string[] Tags, DateTimeOffset UpdatedAt);

    public async Task InitializeAsync()
    {
        _client = await fixture.CreateAuthenticatedClientAsync();

        await _seedLock.WaitAsync();
        try
        {
            if (_seeded) return;

            // Create category; if it already exists (conflict), look it up.
            var createResponse = await _client.PostAsJsonAsync("/categories", new { name = "SearchCat", sortOrder = 0 });
            int categoryId;
            if (createResponse.IsSuccessStatusCode)
            {
                categoryId = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryPayload>>())!.Data!.Id;
            }
            else
            {
                var list = await _client.GetFromJsonAsync<ApiResponse<List<CategoryPayload>>>("/categories");
                categoryId = list!.Data!.Single(c => c.Slug == "searchcat").Id;
            }

            (await _client.PostAsJsonAsync("/cheatsheets", new
            {
                title = "Kubernetes Basics", categoryId, contentType = "markdown",
                content = "pods and deployments", tags = new[] { "searchable" },
            })).EnsureSuccessStatusCode();
            (await _client.PostAsJsonAsync("/cheatsheets", new
            {
                title = "Service Mesh", categoryId, contentType = "markdown",
                content = "istio runs on kubernetes clusters", tags = Array.Empty<string>(),
            })).EnsureSuccessStatusCode();

            _seeded = true;
        }
        finally
        {
            _seedLock.Release();
        }
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
