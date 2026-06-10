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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
        Assert.True(tooBig.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge);
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

    [Fact]
    public async Task Export_downloads_html_with_correct_content_type()
    {
        var (client, categoryId) = await SetupAsync("ImportCat E");
        var create = await client.PostAsJsonAsync("/cheatsheets", new
        { title = "Html Sheet", categoryId, contentType = "html", content = "<h1>Hello</h1>", tags = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<SheetPayload>>())!.Data!.Id;

        var response = await client.GetAsync($"/cheatsheets/{id}/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("html-sheet.html", response.Content.Headers.ContentDisposition!.FileName);
    }
}
