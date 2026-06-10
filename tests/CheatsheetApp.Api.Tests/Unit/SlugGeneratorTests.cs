using CheatsheetApp.Api.Common;

namespace CheatsheetApp.Api.Tests.Unit;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Undo Last Commit", "undo-last-commit")]
    [InlineData("  C#  LINQ   Tricks!  ", "c-linq-tricks")]
    [InlineData("Café déjà-vu", "cafe-deja-vu")]
    [InlineData("docker_compose.v2", "docker-compose-v2")]
    [InlineData("!!!", "untitled")]
    public void Generate_produces_url_safe_slug(string input, string expected)
    {
        var slug = SlugGenerator.Generate(input);

        Assert.Equal(expected, slug);
    }
}
