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
