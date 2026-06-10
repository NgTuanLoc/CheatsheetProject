namespace CheatsheetApp.Api.Data;

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public List<Cheatsheet> Cheatsheets { get; set; } = [];
}
