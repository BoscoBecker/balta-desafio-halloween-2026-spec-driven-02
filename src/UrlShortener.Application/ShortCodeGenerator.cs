using System.Security.Cryptography;

namespace UrlShortener.Application;

public interface IShortCodeGenerator
{
    string Generate();
}

public sealed class ShortCodeGenerator : IShortCodeGenerator
{
    public string Generate()
    {
        Span<char> buffer = stackalloc char[LinkCodes.AutoLength];
        Span<byte> bytes = stackalloc byte[LinkCodes.AutoLength];
        RandomNumberGenerator.Fill(bytes);

        for (var i = 0; i < LinkCodes.AutoLength; i++)
        {
            buffer[i] = LinkCodes.Alphabet[bytes[i] % LinkCodes.Alphabet.Length];
        }

        return new string(buffer);
    }
}
