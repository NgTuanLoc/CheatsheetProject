using System.Text.RegularExpressions;
using CheatsheetApp.Api.Common;
using CheatsheetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed partial class ListCheatsheetsEndpoint : IEndpoint
{
    private const int MaxSearchTerms = 8;

    public void Map(IEndpointRouteBuilder app) => app.MapGet("/cheatsheets", HandleAsync);

    private static async Task<IResult> HandleAsync(
        string? category, string? tag, string? q, AppDbContext db, CancellationToken ct)
    {
        var query = db.Cheatsheets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category.Slug == category);
        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(c => c.Tags.Any(t => t.Slug == tag));

        var tsQuery = string.IsNullOrWhiteSpace(q) ? null : ToPrefixTsQuery(q);
        // EF Core cannot reuse server-side expressions; ToTsQuery is called twice intentionally.
        query = string.IsNullOrEmpty(tsQuery)
            ? query.OrderByDescending(c => c.UpdatedAt)
            : query
                .Where(c => c.SearchVector.Matches(EF.Functions.ToTsQuery("english", tsQuery)))
                .OrderByDescending(c => c.SearchVector.Rank(EF.Functions.ToTsQuery("english", tsQuery)));

        var items = await query
            .Select(c => new CheatsheetSummary(
                c.Id, c.Title, c.Slug, c.Category.Slug, c.Category.Name, c.ContentType,
                c.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToArray(), c.UpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(ApiResponse<List<CheatsheetSummary>>.Ok(items));
    }

    /// <summary>Sanitizes free text into a safe tsquery: lexemes ANDed, each prefix-matched.</summary>
    private static string ToPrefixTsQuery(string input)
    {
        var terms = TermPattern().Matches(input.ToLowerInvariant())
            .Select(m => m.Value)
            .Take(MaxSearchTerms)
            .ToArray();
        return terms.Length == 0 ? "" : string.Join(" & ", terms.Select(t => $"{t}:*"));
    }

    [GeneratedRegex("[a-z0-9]+")]
    private static partial Regex TermPattern();
}
