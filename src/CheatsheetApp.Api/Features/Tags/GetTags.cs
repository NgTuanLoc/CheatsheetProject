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
