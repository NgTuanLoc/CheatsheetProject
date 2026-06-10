using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CheatsheetApp.Api.Common;

public static partial class SlugGenerator
{
    public static string Generate(string input)
    {
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsAsciiLetterOrDigit(ch))
                sb.Append(ch);
            else if (ch is ' ' or '-' or '_' or '.')
                sb.Append('-');
        }

        var slug = CollapseDashes().Replace(sb.ToString(), "-").Trim('-');
        return slug.Length == 0 ? "untitled" : slug;
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseDashes();
}
