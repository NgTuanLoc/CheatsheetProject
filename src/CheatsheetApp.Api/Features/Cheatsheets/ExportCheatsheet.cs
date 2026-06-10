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
