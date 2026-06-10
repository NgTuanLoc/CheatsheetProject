using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class UpdateCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/cheatsheets/{id:int}", HandleAsync)
            .AddEndpointFilter<ValidationFilter<SaveCheatsheetRequest>>();

    private static async Task<IResult> HandleAsync(
        int id, SaveCheatsheetRequest request, AppDbContext db, CancellationToken ct)
    {
        var sheet = await db.Cheatsheets
            .Include(c => c.Category)
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == id, ct);
        if (sheet is null)
            return Results.NotFound(ApiResponse<object>.Fail("Cheatsheet not found."));

        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
            return Results.NotFound(ApiResponse<object>.Fail("Category not found."));

        sheet.Title = request.Title.Trim();
        sheet.CategoryId = category.Id;
        sheet.Category = category;
        sheet.Slug = await SheetSlug.GenerateUniqueAsync(db, category.Id, request.Title, id, ct);
        sheet.ContentType = request.ContentType;
        sheet.Content = request.Content;
        sheet.UpdatedAt = DateTimeOffset.UtcNow;
        sheet.Tags.Clear();
        sheet.Tags.AddRange(await TagResolver.ResolveAsync(db, request.Tags, ct));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true)
        {
            return Results.Conflict(ApiResponse<object>.Fail("A cheatsheet with this title already exists in this category."));
        }

        return Results.Ok(ApiResponse<CheatsheetDetail>.Ok(CheatsheetDetail.From(sheet)));
    }
}
