using System.Text.RegularExpressions;

namespace UrlShortener.Application;

public static partial class LinkCodes
{
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    public const int AutoLength = 7;
    public const int MinCustomLength = 4;
    public const int MaxCustomLength = 20;

    public static bool IsValid(string code) => CustomCodePattern().IsMatch(code);

    [GeneratedRegex("^[A-Z1-9]{4,20}$")]
    private static partial Regex CustomCodePattern();
}
