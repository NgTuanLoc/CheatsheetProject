using NpgsqlTypes;

namespace CheatsheetApp.Api.Data;

public static class ContentTypes
{
    public const string Markdown = "markdown";
    public const string Html = "html";
    public static readonly string[] All = [Markdown, Html];
}

public sealed class Cheatsheet
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string ContentType { get; set; }
    public required string Content { get; set; }
    public NpgsqlTsVector SearchVector { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<Tag> Tags { get; set; } = [];
}
