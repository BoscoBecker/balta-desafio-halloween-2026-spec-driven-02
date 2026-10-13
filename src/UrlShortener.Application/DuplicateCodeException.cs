namespace UrlShortener.Application;

public sealed class DuplicateCodeException : Exception
{
    public DuplicateCodeException(string code)
        : base($"O código '{code}' já está em uso.")
    {
        Code = code;
    }

    public string Code { get; }
}
