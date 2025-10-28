using System.Diagnostics.CodeAnalysis;

namespace CommonLibrary.Extensions;
public static class StringExtensions
{
    public static string? Capitalise(this string? s)
    {
        if (string.IsNullOrEmpty(s) || char.IsUpper(s[0])) return s;

        return s[0..1].ToUpper() + s[1..];
    }

    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? s)
    {
        return string.IsNullOrEmpty(s);
    }
}
