using CheatsheetApp.Api.Data;

namespace CheatsheetApp.Api.Features.Categories;

public sealed record CategoryResponse(int Id, string Name, string Slug, string? Icon, int SortOrder)
{
    public static CategoryResponse From(Category c) => new(c.Id, c.Name, c.Slug, c.Icon, c.SortOrder);
}

public sealed record SaveCategoryRequest(string Name, string? Icon, int SortOrder);
