using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class UpdateCategoryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/categories/{id:int}", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCategoryRequest>>();

    private static async Task<IResult> HandleAsync(
        int id, SaveCategoryRequest request, AppDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        var slug = SlugGenerator.Generate(request.Name);
        if (await db.Categories.AnyAsync(c => c.Slug == slug && c.Id != id, ct))
            return Results.Conflict(ApiResponse<object>.Fail("A category with this name already exists."));

        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.Icon = request.Icon;
        category.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<CategoryResponse>.Ok(CategoryResponse.From(category)));
    }
}
