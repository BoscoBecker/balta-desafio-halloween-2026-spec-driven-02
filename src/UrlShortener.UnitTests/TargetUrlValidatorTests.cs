using UrlShortener.Application;

namespace UrlShortener.UnitTests;

public class TargetUrlValidatorTests
{
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path?q=1")]
    public void Accepts_http_and_https_public_urls(string url)
    {
        var valid = TargetUrlValidator.TryValidate(url, out var error);

        Assert.True(valid);
        Assert.Empty(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    [InlineData("http://localhost/app")]
    [InlineData("http://127.0.0.1/app")]
    [InlineData("http://192.168.0.10/app")]
    [InlineData("http://10.0.0.8/app")]
    public void Rejects_invalid_or_non_public_urls(string? url)
    {
        var valid = TargetUrlValidator.TryValidate(url, out var error);

        Assert.False(valid);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Rejects_urls_longer_than_2048_characters()
    {
        var url = "https://example.com/" + new string('a', 2048);

        var valid = TargetUrlValidator.TryValidate(url, out var error);

        Assert.False(valid);
        Assert.Contains("2048", error);
    }
}
