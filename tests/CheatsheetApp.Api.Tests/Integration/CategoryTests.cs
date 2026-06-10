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
