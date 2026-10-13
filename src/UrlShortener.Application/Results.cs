namespace UrlShortener.Application;

public sealed record CreateLinkCommand(string TargetUrl, string? Code, DateTimeOffset? ExpiresAtUtc);

public sealed record CreatedLink(string Code, string ManagementKey);

public sealed record RedirectResult(string TargetUrl);

public sealed record LinkStats(string Code, string TargetUrl, long AccessCount, DateTime CreatedAtUtc, DateTime? ExpiresAtUtc);

public sealed record ApplicationError(int StatusCode, string Title, string Detail);

public sealed class ApplicationResult<T>
{
    private ApplicationResult(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private ApplicationResult(ApplicationError error)
    {
        IsSuccess = false;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public ApplicationError? Error { get; }

    public static ApplicationResult<T> Success(T value) => new(value);
    public static ApplicationResult<T> Failure(ApplicationError error) => new(error);
}
