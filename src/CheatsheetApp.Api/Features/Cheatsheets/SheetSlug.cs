using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public static class SheetSlug
{
    /// <summary>Generates a slug unique within the category, appending -2, -3, ... on collision.</summary>
    public static async Task<string> GenerateUniqueAsync(
        AppDbContext db, int categoryId, string title, int? excludeId, CancellationToken ct)
    {
        var baseSlug = SlugGenerator.Generate(title);
        var slug = baseSlug;
        var suffix = 2;
        while (await db.Cheatsheets.AnyAsync(
            c => c.CategoryId == categoryId && c.Slug == slug && (excludeId == null || c.Id != excludeId), ct))
        {
            slug = $"{baseSlug}-{suffix++}";
        }
        return slug;
    }
}
