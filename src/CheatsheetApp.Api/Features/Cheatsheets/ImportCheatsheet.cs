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
