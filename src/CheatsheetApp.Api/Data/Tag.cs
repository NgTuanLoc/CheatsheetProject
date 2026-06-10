namespace CheatsheetApp.Api.Data;

public sealed class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public List<Cheatsheet> Cheatsheets { get; set; } = [];
}
