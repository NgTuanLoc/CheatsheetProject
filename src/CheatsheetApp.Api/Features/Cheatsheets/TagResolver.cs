using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public static class TagResolver
{
    /// <summary>Finds existing tags by slug or creates new ones. Caller saves changes.</summary>
    public static async Task<List<Tag>> ResolveAsync(
        AppDbContext db, IEnumerable<string> names, CancellationToken ct)
    {
        var distinct = names
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .DistinctBy(SlugGenerator.Generate)
            .ToList();

        var slugs = distinct.Select(SlugGenerator.Generate).ToList();
        var existing = await db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync(ct);

        var result = new List<Tag>(existing);
        foreach (var name in distinct)
        {
            var slug = SlugGenerator.Generate(name);
            if (existing.All(t => t.Slug != slug))
            {
                var tag = new Tag { Name = name, Slug = slug };
                db.Tags.Add(tag);
                result.Add(tag);
            }
        }
        return result;
    }
}
