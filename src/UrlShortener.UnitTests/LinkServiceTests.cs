using UrlShortener.Application;
using UrlShortener.Application.Abstractions;
using UrlShortener.Domain;

namespace UrlShortener.UnitTests;

public class LinkServiceTests
{
    [Fact]
    public async Task Create_rejects_invalid_url()
    {
        var service = CreateService(out _);

        var result = await service.CreateAsync(new CreateLinkCommand("ftp://example.com", null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Create_with_duplicate_custom_code_returns_conflict()
    {
        var service = CreateService(out var repository);
        repository.Links["HALLOWEEN"] = new Link
        {
            Id = Guid.NewGuid(),
            Code = "HALLOWEEN",
            TargetUrl = "https://example.com",
            CreatedAtUtc = DateTime.UtcNow,
            ManagementKeyHash = "hash"
        };

        var result = await service.CreateAsync(
            new CreateLinkCommand("https://example.com/novo", "HALLOWEEN", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Redirect_does_not_increment_expired_link()
    {
        var service = CreateService(out var repository, new DateTime(2026, 10, 14, 0, 0, 1, DateTimeKind.Utc));
        repository.Links["EXPIRED1"] = new Link
        {
            Id = Guid.NewGuid(),
            Code = "EXPIRED1",
            TargetUrl = "https://example.com",
            CreatedAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 10, 13, 23, 59, 59, DateTimeKind.Utc),
            AccessCount = 3,
            ManagementKeyHash = "hash"
        };

        var result = await service.RedirectAsync("EXPIRED1", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(410, result.Error!.StatusCode);
        Assert.Equal(3, repository.Links["EXPIRED1"].AccessCount);
        Assert.Equal(0, repository.Increments);
    }

    [Fact]
    public async Task Redirect_increments_valid_link_and_caches_it()
    {
        var service = CreateService(out var repository, cache: out var cache);
        repository.Links["VALID12"] = new Link
        {
            Id = Guid.NewGuid(),
            Code = "VALID12",
            TargetUrl = "https://example.com/destino",
            CreatedAtUtc = DateTime.UtcNow,
            ManagementKeyHash = "hash"
        };

        var first = await service.RedirectAsync("VALID12", CancellationToken.None);
        var second = await service.RedirectAsync("VALID12", CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, repository.Increments);
        Assert.True(cache.TryGet("VALID12", out _));
    }

    [Fact]
    public async Task Stats_with_wrong_key_returns_unauthorized()
    {
        var keys = new FakeManagementKeys();
        var service = CreateService(out var repository, keys: keys);
        repository.Links["STATS12"] = new Link
        {
            Id = Guid.NewGuid(),
            Code = "STATS12",
            TargetUrl = "https://example.com",
            CreatedAtUtc = DateTime.UtcNow,
            ManagementKeyHash = keys.Hash("correta")
        };

        var result = await service.GetStatsAsync("STATS12", "errada", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.Error!.StatusCode);
    }

    private static LinkService CreateService(
        out InMemoryLinkRepository repository,
        DateTime? utcNow = null,
        FakeManagementKeys? keys = null) =>
        CreateService(out repository, out _, utcNow, keys);

    private static LinkService CreateService(
        out InMemoryLinkRepository repository,
        out InMemoryLinkRedirectCache cache,
        DateTime? utcNow = null,
        FakeManagementKeys? keys = null)
    {
        repository = new InMemoryLinkRepository();
        cache = new InMemoryLinkRedirectCache();
        return new LinkService(
            repository,
            new ShortCodeGenerator(),
            keys ?? new FakeManagementKeys(),
            cache,
            new FixedClock(utcNow ?? DateTime.UtcNow));
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class FakeManagementKeys : IManagementKeyService
    {
        public string Generate() => "generated-key";
        public string Hash(string managementKey) => $"hash:{managementKey}";
        public bool Matches(string managementKey, string hash) => Hash(managementKey) == hash;
    }

    private sealed class InMemoryLinkRedirectCache : ILinkRedirectCache
    {
        private readonly Dictionary<string, CachedLink> _items = new();

        public bool TryGet(string code, out CachedLink? cached) => _items.TryGetValue(code, out cached);
        public void Set(string code, CachedLink cached) => _items[code] = cached;
        public void Remove(string code) => _items.Remove(code);
    }

    private sealed class InMemoryLinkRepository : ILinkRepository
    {
        public Dictionary<string, Link> Links { get; } = new();
        public int Increments { get; private set; }

        public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(Links.GetValueOrDefault(code));

        public Task<bool> ExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(Links.ContainsKey(code));

        public Task AddAsync(Link link, CancellationToken cancellationToken)
        {
            Links[link.Code] = link;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<int> IncrementAccessCountAsync(string code, DateTime utcNow, CancellationToken cancellationToken)
        {
            if (!Links.TryGetValue(code, out var link) || link.IsExpired(utcNow))
            {
                return Task.FromResult(0);
            }

            link.AccessCount++;
            Increments++;
            return Task.FromResult(1);
        }
    }
}
