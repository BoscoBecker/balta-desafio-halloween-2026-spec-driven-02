using UrlShortener.Application;

namespace UrlShortener.Api.Endpoints;

public static class RedirectEndpoints
{
    public static IEndpointRouteBuilder MapRedirectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{code}", Redirect);

        return app;
    }

    private static async Task<IResult> Redirect(
        string code,
        LinkService links,
        CancellationToken cancellationToken)
    {
        var result = await links.RedirectAsync(code, cancellationToken);
        return HttpResultMapper.ToHttpResult(
            result,
            redirect => Results.Redirect(redirect.TargetUrl, permanent: false));
    }
}
