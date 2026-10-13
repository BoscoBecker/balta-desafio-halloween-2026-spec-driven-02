using System.Net;
using System.Net.Sockets;

namespace UrlShortener.Application;

public static class TargetUrlValidator
{
    public const int MaxLength = 2048;

    public static bool TryValidate(string? targetUrl, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            error = "A URL de destino é obrigatória.";
            return false;
        }

        if (targetUrl.Length > MaxLength)
        {
            error = "A URL de destino não pode ter mais de 2048 caracteres.";
            return false;
        }

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "A URL de destino precisa usar http ou https.";
            return false;
        }

        if (!IsPubliclyAccessible(uri))
        {
            error = "A URL de destino precisa ser publicamente acessível.";
            return false;
        }

        return true;
    }

    private static bool IsPubliclyAccessible(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return false;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!IPAddress.TryParse(uri.Host, out var address))
        {
            return true;
        }

        return !IsPrivate(address);
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            127 => true,
            169 when bytes[1] == 254 => true,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
            192 when bytes[1] == 168 => true,
            _ => false
        };
    }
}
