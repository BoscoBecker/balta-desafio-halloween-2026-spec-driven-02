using UrlShortener.Application;

namespace UrlShortener.Api.Endpoints;

internal static class HttpResultMapper
{
    public static IResult ToHttpResult<T>(ApplicationResult<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var error = result.Error!;
        return Results.Problem(
            title: error.Title,
            detail: error.Detail,
            statusCode: error.StatusCode);
    }
}
