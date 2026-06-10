using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed class DeleteCheatsheetEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/cheatsheets/{id:int}", HandleAsync);

    private static async Task<IResult> HandleAsync(int id, AppDbContext db, CancellationToken ct)
    {
        var sheet = await db.Cheatsheets.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (sheet is null)
            return Results.NotFound(ApiResponse<object>.Fail("Cheatsheet not found."));

        db.Cheatsheets.Remove(sheet);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object?>.Ok(null));
    }
}
