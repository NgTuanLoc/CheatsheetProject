using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class SaveCheatsheetRequestValidator : AbstractValidator<SaveCheatsheetRequest>
{
    public SaveCheatsheetRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.ContentType)
            .Must(t => ContentTypes.All.Contains(t))
            .WithMessage("ContentType must be 'markdown' or 'html'.");
        RuleFor(r => r.Content).NotEmpty().MaximumLength(500_000);
        RuleFor(r => r.Tags).NotNull().Must(t => t.Length <= 20).WithMessage("No more than 20 tags per cheatsheet.");
        RuleForEach(r => r.Tags).MaximumLength(60);
    }
}

public sealed class CreateCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/cheatsheets", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCheatsheetRequest>>();

    private static async Task<IResult> HandleAsync(
        SaveCheatsheetRequest request, AppDbContext db, CancellationToken ct)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        var now = DateTimeOffset.UtcNow;
        var sheet = new Cheatsheet
        {
            Title = request.Title.Trim(),
            CategoryId = category.Id,
            Category = category,
            Slug = await SheetSlug.GenerateUniqueAsync(db, category.Id, request.Title, null, ct),
            ContentType = request.ContentType,
            Content = request.Content,
            CreatedAt = now,
            UpdatedAt = now,
            Tags = await TagResolver.ResolveAsync(db, request.Tags, ct),
        };
        db.Cheatsheets.Add(sheet);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true)
        {
            return Results.Conflict(ApiResponse<object>.Fail("A cheatsheet with this title already exists in this category."));
        }

        return Results.Created($"/cheatsheets/{category.Slug}/{sheet.Slug}",
            ApiResponse<CheatsheetDetail>.Ok(CheatsheetDetail.From(sheet)));
    }
}
