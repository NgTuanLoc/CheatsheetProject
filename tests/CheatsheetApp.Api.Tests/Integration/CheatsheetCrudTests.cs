using System.Net;
using System.Net.Http.Json;
using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Integration;

[Collection("api")]
public class CheatsheetCrudTests(ApiFixture fixture)
{
    public sealed record SheetPayload(
        int Id, string Title, string Slug, string CategorySlug, string CategoryName, string ContentType,
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
        Assert.False(string.IsNullOrEmpty(sheet.CategoryName));
        Assert.Equal(new[] { "git", "Recovery" }, sheet.Tags); // sorted ascending by name (culture-aware: lowercase before uppercase)
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
