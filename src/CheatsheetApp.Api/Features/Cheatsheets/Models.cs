using CheatsheetApp.Api.Data;

namespace CheatsheetApp.Api.Features.Cheatsheets;

public sealed record CheatsheetDetail(
    int Id, string Title, string Slug, string CategorySlug, string CategoryName,
    string ContentType, string Content, string[] Tags,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public static CheatsheetDetail From(Cheatsheet c) => new(
        c.Id, c.Title, c.Slug, c.Category.Slug, c.Category.Name,
        c.ContentType, c.Content,
        c.Tags.OrderBy(t => t.Name).Select(t => t.Name).ToArray(),
        c.CreatedAt, c.UpdatedAt);
}

public sealed record CheatsheetSummary(
    int Id, string Title, string Slug, string CategorySlug, string CategoryName,
    string ContentType, string[] Tags, DateTimeOffset UpdatedAt);

public sealed record SaveCheatsheetRequest(
    string Title, int CategoryId, string ContentType, string Content, string[] Tags);
