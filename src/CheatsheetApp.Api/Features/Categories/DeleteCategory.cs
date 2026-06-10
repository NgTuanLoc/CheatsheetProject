using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class DeleteCategoryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/categories/{id:int}", HandleAsync);

    private static async Task<IResult> HandleAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        if (await db.Cheatsheets.AnyAsync(c => c.CategoryId == id, ct))
            return Results.Conflict(ApiResponse<object>.Fail(
                "Category still contains cheatsheets. Reassign them first."));

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object?>.Ok(null));
    }
}
