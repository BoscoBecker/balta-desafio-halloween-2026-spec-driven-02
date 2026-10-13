using UrlShortener.Application;

namespace UrlShortener.UnitTests;

public class ExpirationRulesTests
{
    [Fact]
    public void Normalize_keeps_link_valid_until_end_of_utc_date()
    {
        var input = new DateTimeOffset(2026, 10, 13, 8, 30, 0, TimeSpan.Zero);

        var normalized = ExpirationRules.NormalizeToEndOfUtcDate(input);

        Assert.Equal(new DateTime(2026, 10, 13, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), normalized);
    }

    [Fact]
    public void Past_utc_date_is_rejected()
    {
        var now = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
        var yesterday = new DateTime(2026, 10, 12, 23, 59, 59, DateTimeKind.Utc);

        Assert.True(ExpirationRules.IsInThePast(yesterday, now));
        Assert.False(ExpirationRules.IsInThePast(now, now));
    }
}
