using UrlShortener.Api.Contracts;
using UrlShortener.Application;

namespace UrlShortener.Api.Endpoints;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/links");

        group.MapPost(string.Empty, CreateLink);
        group.MapGet("/{code}/stats", GetStats);

        return app;
    }

    private static async Task<IResult> CreateLink(
        CreateLinkRequest request,
        LinkService links,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var result = await links.CreateAsync(
            new CreateLinkCommand(request.TargetUrl, request.Code, request.ExpiresAtUtc),
            cancellationToken);

        return HttpResultMapper.ToHttpResult(result, created =>
        {
            var shortUrl = $"{http.Request.Scheme}://{http.Request.Host}/{created.Code}";
            return Results.Created(shortUrl, new CreateLinkResponse
            {
                ShortUrl = shortUrl,
                Code = created.Code,
                ManagementKey = created.ManagementKey
            });
        });
    }

    private static async Task<IResult> GetStats(
        string code,
        LinkService links,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        http.Request.Headers.TryGetValue("X-Management-Key", out var key);
        var result = await links.GetStatsAsync(code, key.ToString(), cancellationToken);

        return HttpResultMapper.ToHttpResult(result, stats => Results.Ok(new LinkStatsResponse
        {
            Code = stats.Code,
            TargetUrl = stats.TargetUrl,
            AccessCount = stats.AccessCount,
            CreatedAtUtc = stats.CreatedAtUtc,
            ExpiresAtUtc = stats.ExpiresAtUtc
        }));
    }
}
