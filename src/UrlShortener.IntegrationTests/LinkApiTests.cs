using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UrlShortener.IntegrationTests;

public class LinkApiTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public LinkApiTests()
    {
        _client = _factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Create_link_returns_201_with_short_url()
    {
        var response = await _client.PostAsJsonAsync("/api/links", new
        {
            targetUrl = "https://example.com/halloween"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreatedLinkDto>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(7, payload.Code.Length);
        Assert.False(string.IsNullOrWhiteSpace(payload.ManagementKey));
        Assert.Contains(payload.Code, payload.ShortUrl);
    }

    [Fact]
    public async Task Duplicate_custom_code_returns_409()
    {
        var body = new { targetUrl = "https://example.com/um", code = "FESTA26" };
        var first = await _client.PostAsJsonAsync("/api/links", body);
        var second = await _client.PostAsJsonAsync("/api/links", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Accessing_short_link_returns_302_to_target()
    {
        var created = await CreateAsync("https://example.com/destino");

        var response = await _client.GetAsync($"/{created.Code}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://example.com/destino", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Concurrent_accesses_do_not_lose_counts()
    {
        var created = await CreateAsync("https://example.com/contador");
        const int hits = 40;

        var tasks = Enumerable.Range(0, hits)
            .Select(_ => _client.GetAsync($"/{created.Code}"))
            .ToArray();
        await Task.WhenAll(tasks);

        using var statsRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{created.Code}/stats");
        statsRequest.Headers.Add("X-Management-Key", created.ManagementKey);
        var stats = await _client.SendAsync(statsRequest);
        stats.EnsureSuccessStatusCode();
        var payload = await stats.Content.ReadFromJsonAsync<StatsDto>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(hits, payload.AccessCount);
    }

    [Fact]
    public async Task Wrong_management_key_returns_401()
    {
        var created = await CreateAsync("https://example.com/stats");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{created.Code}/stats");
        request.Headers.Add("X-Management-Key", "chave-errada");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_management_key_returns_stats()
    {
        var created = await CreateAsync("https://example.com/stats-ok");
        await _client.GetAsync($"/{created.Code}");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{created.Code}/stats");
        request.Headers.Add("X-Management-Key", created.ManagementKey);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<StatsDto>(JsonOptions);
        Assert.Equal(1, payload!.AccessCount);
    }

    [Fact]
    public async Task Expired_link_returns_410_and_does_not_count()
    {
        var expires = DateTimeOffset.UtcNow;
        var response = await _client.PostAsJsonAsync("/api/links", new
        {
            targetUrl = "https://example.com/expira",
            expiresAtUtc = expires
        });
        var created = await response.Content.ReadFromJsonAsync<CreatedLinkDto>(JsonOptions);
        Assert.NotNull(created);

        _factory.Clock.UtcNow = expires.UtcDateTime.Date.AddDays(1);

        var redirect = await _client.GetAsync($"/{created.Code}");
        Assert.Equal(HttpStatusCode.Gone, redirect.StatusCode);

        using var statsRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{created.Code}/stats");
        statsRequest.Headers.Add("X-Management-Key", created.ManagementKey);
        var stats = await _client.SendAsync(statsRequest);
        var payload = await stats.Content.ReadFromJsonAsync<StatsDto>(JsonOptions);
        Assert.Equal(0, payload!.AccessCount);
    }

    [Fact]
    public async Task Invalid_url_returns_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/api/links", new { targetUrl = "ftp://example.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<CreatedLinkDto> CreateAsync(string targetUrl)
    {
        var response = await _client.PostAsJsonAsync("/api/links", new { targetUrl });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedLinkDto>(JsonOptions);
        return payload!;
    }

    private sealed class CreatedLinkDto
    {
        public string ShortUrl { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ManagementKey { get; set; } = string.Empty;
    }

    private sealed class StatsDto
    {
        public long AccessCount { get; set; }
    }
}
