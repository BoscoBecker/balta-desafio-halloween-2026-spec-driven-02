namespace UrlShortener.Application;

public static class ExpirationRules
{
    public static DateTime? NormalizeToEndOfUtcDate(DateTimeOffset? expiresAtUtc)
    {
        if (expiresAtUtc is null)
        {
            return null;
        }

        var date = expiresAtUtc.Value.UtcDateTime.Date;
        return DateTime.SpecifyKind(date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
    }

    public static bool IsInThePast(DateTime expiresAtUtc, DateTime utcNow) =>
        expiresAtUtc.Date < utcNow.Date;
}
