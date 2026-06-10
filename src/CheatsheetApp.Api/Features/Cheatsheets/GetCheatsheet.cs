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
