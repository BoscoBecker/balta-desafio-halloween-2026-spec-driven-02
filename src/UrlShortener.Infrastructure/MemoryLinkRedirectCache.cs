using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Application.Abstractions;

namespace UrlShortener.Infrastructure;

public sealed class MemoryLinkRedirectCache(IMemoryCache cache) : ILinkRedirectCache
{
    public bool TryGet(string code, out CachedLink? cached) =>
        cache.TryGetValue(code, out cached);

    public void Set(string code, CachedLink cached)
    {
        cache.Set(
            code,
            cached,
            new MemoryCacheEntryOptions
            {
                Size = 1,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            });
    }

    public void Remove(string code) => cache.Remove(code);
}
