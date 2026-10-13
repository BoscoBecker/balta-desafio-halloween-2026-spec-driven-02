namespace UrlShortener.Api.Contracts;

public sealed class CreateLinkRequest
{
    public string TargetUrl { get; set; } = string.Empty;
    public string? Code { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

public sealed class CreateLinkResponse
{
    public required string ShortUrl { get; init; }
    public required string Code { get; init; }
    public required string ManagementKey { get; init; }
}

public sealed class LinkStatsResponse
{
    public required string Code { get; init; }
    public required string TargetUrl { get; init; }
    public required long AccessCount { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}
