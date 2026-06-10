using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class GetCategoriesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app.MapGet("/categories", HandleAsync);

    private static async Task<IResult> HandleAsync(AppDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => CategoryResponse.From(c))
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<List<CategoryResponse>>.Ok(categories));
    }
}
