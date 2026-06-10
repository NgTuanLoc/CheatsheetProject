using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Categories;

public sealed class SaveCategoryRequestValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Icon).MaximumLength(20);
    }
}

public sealed class CreateCategoryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/categories", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCategoryRequest>>();

    private static async Task<IResult> HandleAsync(
        SaveCategoryRequest request, AppDbContext db, CancellationToken ct)
    {
        var slug = SlugGenerator.Generate(request.Name);
        if (await db.Categories.AnyAsync(c => c.Slug == slug, ct))
            return Results.Conflict(ApiResponse<object>.Fail("A category with this name already exists."));

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/categories/{category.Id}",
            ApiResponse<CategoryResponse>.Ok(CategoryResponse.From(category)));
    }
}
