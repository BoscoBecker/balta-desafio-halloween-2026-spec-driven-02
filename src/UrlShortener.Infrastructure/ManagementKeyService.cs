using System.Security.Cryptography;
using System.Text;
using UrlShortener.Application.Abstractions;

namespace UrlShortener.Infrastructure;

public sealed class ManagementKeyService : IManagementKeyService
{
    public string Generate()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    public string Hash(string managementKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(managementKey));
        return Convert.ToHexString(bytes);
    }

    public bool Matches(string managementKey, string hash)
    {
        var computed = Hash(managementKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(hash));
    }
}
