namespace UrlShortener.Application.Abstractions;

public sealed record CachedLink(string TargetUrl, DateTime? ExpiresAtUtc);

public interface ILinkRedirectCache
{
    bool TryGet(string code, out CachedLink? cached);
    void Set(string code, CachedLink cached);
    void Remove(string code);
}
